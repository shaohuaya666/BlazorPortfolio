using BlazorPortfolio.Data;
using BlazorPortfolio.Models;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BlazorPortfolio.Services.Email;

/// <summary>
/// 邮件通知后台服务（Hosted Service）。<br/>
/// 职责：消费 <see cref="EmailQueueService"/> 中的待发邮件、调用 SMTP 发送、失败按退避策略重试、
/// 回写 ChatMessages 的邮件状态，并在启动时补偿上次进程退出前未发出的记录。
/// </summary>
public class EmailNotificationService : BackgroundService
{
    /// <summary>无待重试任务时，等待新邮件的最大空闲时间</summary>
    private static readonly TimeSpan MaxIdleWait = TimeSpan.FromSeconds(5);

    private readonly EmailQueueService _queue;
    private readonly IEmailSender _sender;
    private readonly IDbContextFactory<PortfolioDbContext> _dbFactory;
    private readonly IOptions<SmtpOptions> _options;
    private readonly ILogger<EmailNotificationService> _logger;

    /// <summary>等待到期的重试任务（仅在内存中排队，进程退出后由数据库状态补偿）</summary>
    private readonly List<RetryItem> _retries = [];

    public EmailNotificationService(
        EmailQueueService queue,
        IEmailSender sender,
        IDbContextFactory<PortfolioDbContext> dbFactory,
        IOptions<SmtpOptions> options,
        ILogger<EmailNotificationService> logger)
    {
        _queue = queue;
        _sender = sender;
        _dbFactory = dbFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _options.Value;
        if (!options.IsConfigured)
        {
            _logger.LogWarning(
                "邮件通知未启用或 Smtp 配置不完整（Enabled={Enabled}），联系表单消息仅写入数据库，不会发送邮件。",
                options.Enabled);
            return;
        }

        _logger.LogInformation(
            "EmailNotificationService 已启动：SMTP {Host}:{Port}，收件地址 {ToAddress}",
            options.Host, options.Port, options.ToAddress);

        await CompensatePendingAsync(stoppingToken);

        try
        {
            await ConsumeLoopAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // 正常关闭
        }

        var remaining = _queue.PendingCount;
        if (remaining > 0 || _retries.Count > 0)
        {
            _logger.LogInformation(
                "服务停止时仍有 {Queued} 条邮件待发送、{Retry} 条待重试，将在下次启动时通过状态补偿补发",
                remaining, _retries.Count);
        }

        _logger.LogInformation("EmailNotificationService 已停止");
    }

    /// <summary>
    /// 消费主循环：优先处理已到期的重试任务，否则等待新邮件入队。
    /// </summary>
    private async Task ConsumeLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var retry = TakeDueRetry();
            if (retry is not null)
            {
                await ProcessAsync(retry.Notification, retry.Attempts, ct);
                continue;
            }

            var notification = await WaitNextAsync(ct);
            if (notification is null)
                continue;

            await ProcessAsync(notification, attempts: 0, ct);
        }
    }

    /// <summary>
    /// 等待下一条待发邮件；若有重试任务待到期，则以到期时间为上限超时返回 null。
    /// </summary>
    private async Task<EmailNotification?> WaitNextAsync(CancellationToken ct)
    {
        var wait = MaxIdleWait;
        if (NextRetryDue() is { } due)
        {
            var remaining = due - DateTime.Now;
            wait = remaining <= TimeSpan.Zero ? TimeSpan.Zero : (remaining < MaxIdleWait ? remaining : MaxIdleWait);
        }

        if (_retries.Count > 0)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(wait);
            try
            {
                if (!await _queue.WaitToReadAsync(timeoutCts.Token))
                    return null;
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                return null;
            }
        }
        else if (!await _queue.WaitToReadAsync(ct))
        {
            return null;
        }

        return _queue.TryDequeue(out var notification) ? notification : null;
    }

    /// <summary>
    /// 发送单封邮件并处理成功/失败状态流转。
    /// </summary>
    /// <param name="notification">邮件通知快照</param>
    /// <param name="attempts">已完成的尝试次数</param>
    /// <param name="ct">停止令牌</param>
    private async Task ProcessAsync(EmailNotification notification, int attempts, CancellationToken ct)
    {
        attempts++;
        var options = _options.Value;

        try
        {
            await _sender.SendContactNotificationAsync(notification, ct);

            await TryUpdateStatusAsync(notification.MessageId, ChatMessage.EmailStatusSent, attempts, DateTime.Now, ct);
            _logger.LogInformation(
                "联系表单邮件发送成功：消息 {MessageId}，提交者 {Email}，第 {Attempts} 次尝试",
                notification.MessageId, notification.SenderEmail, attempts);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // 停机中：未发送完成，状态保持待发送，下次启动补偿
            throw;
        }
        catch (Exception ex)
        {
            var transient = IsTransient(ex);
            var canRetry = transient && attempts <= options.MaxRetryCount;

            if (canRetry)
            {
                var delay = GetRetryDelay(attempts, options);
                _retries.Add(new RetryItem(notification, attempts, DateTime.Now.Add(delay)));

                await TryUpdateStatusAsync(notification.MessageId, ChatMessage.EmailStatusPending, attempts, null, ct);
                _logger.LogWarning(ex,
                    "联系表单邮件发送失败（第 {Attempts} 次），{Delay} 秒后重试：消息 {MessageId}",
                    attempts, (int)delay.TotalSeconds, notification.MessageId);
            }
            else
            {
                await TryUpdateStatusAsync(notification.MessageId, ChatMessage.EmailStatusFailed, attempts, null, ct);
                _logger.LogError(ex,
                    "联系表单邮件发送最终失败（共 {Attempts} 次，可重试={Transient}）：消息 {MessageId}，提交者 {Email}。请检查 Smtp 配置或按消息 Id 手工补发。",
                    attempts, transient, notification.MessageId, notification.SenderEmail);
            }
        }
    }

    /// <summary>
    /// 启动补偿：扫描数据库中"待发送/失败"且未超出重试次数的联系消息，重新入队。
    /// 保证进程重启或 SMTP 长时间不可用后不丢通知。
    /// </summary>
    private async Task CompensatePendingAsync(CancellationToken ct)
    {
        try
        {
            var options = _options.Value;
            var since = DateTime.Now.AddDays(-Math.Max(1, options.CompensationLookbackDays));

            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var pending = await db.Set<ChatMessage>()
                .AsNoTracking()
                .Where(m => m.MessageType == ChatMessage.ContactMessageType
                            && (m.EmailStatus == ChatMessage.EmailStatusPending
                                || m.EmailStatus == ChatMessage.EmailStatusFailed)
                            && m.EmailAttempts <= options.MaxRetryCount
                            && m.Timestamp >= since)
                .OrderBy(m => m.Id)
                .Take(Math.Max(1, options.CompensationBatchSize))
                .ToListAsync(ct);

            var enqueued = pending.Count(message => _queue.TryEnqueue(EmailNotification.From(message)));

            if (pending.Count > 0)
            {
                _logger.LogInformation(
                    "邮件通知补偿扫描：命中 {Found} 条未发出的联系消息，入队 {Enqueued} 条（去重后）",
                    pending.Count, enqueued);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // 正常关闭
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "邮件通知补偿扫描失败，不影响正常入队与消费");
        }
    }

    /// <summary>
    /// 回写邮件状态；失败仅记录日志，不向上抛出，避免中断消费循环。
    /// </summary>
    private async Task TryUpdateStatusAsync(int messageId, byte status, int attempts, DateTime? notifiedAt, CancellationToken ct)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var entity = await db.Set<ChatMessage>().FirstOrDefaultAsync(m => m.Id == messageId, ct);
            if (entity is null)
            {
                _logger.LogWarning("邮件状态回写时未找到消息记录：{MessageId}", messageId);
                return;
            }

            entity.EmailStatus = status;
            entity.EmailAttempts = attempts;
            if (notifiedAt.HasValue)
                entity.EmailNotifiedAt = notifiedAt;

            await db.SaveChangesAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // 停机中忽略
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "邮件状态回写失败：消息 {MessageId}，目标状态 {Status}", messageId, status);
        }
    }

    /// <summary>
    /// 判断异常是否值得重试：4xx 临时错误/网络异常可重试，鉴权与配置错误重试无意义。
    /// </summary>
    private static bool IsTransient(Exception ex) => ex switch
    {
        SmtpCommandException command => (int)command.StatusCode is >= 400 and < 500,
        AuthenticationException => false,
        ServiceNotAuthenticatedException => false,
        InvalidOperationException => false,
        _ => true
    };

    private static TimeSpan GetRetryDelay(int attempts, SmtpOptions options)
    {
        var delays = options.RetryDelaySeconds;
        if (delays is null || delays.Length == 0)
            return TimeSpan.FromSeconds(60);

        var index = Math.Clamp(attempts - 1, 0, delays.Length - 1);
        return TimeSpan.FromSeconds(Math.Max(5, delays[index]));
    }

    private RetryItem? TakeDueRetry()
    {
        if (_retries.Count == 0)
            return null;

        var now = DateTime.Now;
        var index = _retries.FindIndex(r => r.DueAt <= now);
        if (index < 0)
            return null;

        var item = _retries[index];
        _retries.RemoveAt(index);
        return item;
    }

    private DateTime? NextRetryDue()
    {
        if (_retries.Count == 0)
            return null;

        var earliest = _retries[0].DueAt;
        foreach (var item in _retries)
        {
            if (item.DueAt < earliest)
                earliest = item.DueAt;
        }

        return earliest;
    }

    /// <summary>内存中的待重试任务</summary>
    private sealed record RetryItem(EmailNotification Notification, int Attempts, DateTime DueAt);
}
