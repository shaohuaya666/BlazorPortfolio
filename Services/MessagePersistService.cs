using BlazorPortfolio.Data;
using BlazorPortfolio.Models;
using BlazorPortfolio.Services.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BlazorPortfolio.Services;

/// <summary>
/// 消息持久化后台服务。
/// 每隔固定间隔从内存队列中批量取出消息，写入 MySQL 数据库；
/// 落库成功后，将其中的联系消息转发到邮件队列（不阻塞落库节拍）。
/// </summary>
public class MessagePersistService : BackgroundService
{
    private readonly MessageQueueService _queue;
    private readonly EmailQueueService _emailQueue;
    private readonly IDbContextFactory<PortfolioDbContext> _dbFactory;
    private readonly IOptions<SmtpOptions> _smtpOptions;
    private readonly ILogger<MessagePersistService> _logger;
    private readonly TimeSpan _interval;

    /// <param name="queue">消息队列</param>
    /// <param name="emailQueue">邮件通知队列</param>
    /// <param name="dbFactory">DbContext 工厂</param>
    /// <param name="smtpOptions">邮件配置（用于判断是否启用邮件通知）</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="intervalSeconds">持久化间隔（秒），默认 5 秒</param>
    public MessagePersistService(
        MessageQueueService queue,
        EmailQueueService emailQueue,
        IDbContextFactory<PortfolioDbContext> dbFactory,
        IOptions<SmtpOptions> smtpOptions,
        ILogger<MessagePersistService> logger,
        int intervalSeconds = 5)
    {
        _queue = queue;
        _emailQueue = emailQueue;
        _dbFactory = dbFactory;
        _smtpOptions = smtpOptions;
        _logger = logger;
        _interval = TimeSpan.FromSeconds(intervalSeconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MessagePersistService 已启动，持久化间隔: {Interval}秒", _interval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_interval, stoppingToken);

                // 从队列批量取出消息
                var batch = _queue.DequeueBatch(maxBatchSize: 100);

                if (batch.Count == 0)
                    continue;

                await SaveBatchAsync(batch, stoppingToken);

                _logger.LogDebug("成功持久化 {Count} 条消息，队列剩余: {Pending}",
                    batch.Count, _queue.PendingCount);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // 正常关闭，忽略
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "消息持久化失败，将在下一个周期重试");
            }
        }

        // 关闭前尝试将剩余消息刷入数据库
        await DrainRemainingAsync();
        _logger.LogInformation("MessagePersistService 已停止");
    }

    /// <summary>
    /// 批量保存消息到数据库，并把其中的联系消息转发到邮件队列。
    /// </summary>
    private async Task SaveBatchAsync(List<ChatMessage> batch, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        db.Set<ChatMessage>().AddRange(batch);
        await db.SaveChangesAsync(ct);

        // 只有落库成功（Id 已回填）后才转发邮件，保证邮件通知只针对已持久化的消息
        ForwardContactNotifications(batch);
    }

    /// <summary>
    /// 将本次落库的联系消息转发到邮件队列。
    /// 邮件未启用或队列已满/重复时静默跳过，不影响消息持久化。
    /// </summary>
    private void ForwardContactNotifications(List<ChatMessage> batch)
    {
        if (!_smtpOptions.Value.IsConfigured)
            return;

        foreach (var message in batch)
        {
            if (message.MessageType != ChatMessage.ContactMessageType || message.Id <= 0)
                continue;

            if (!_emailQueue.TryEnqueue(EmailNotification.From(message)))
            {
                _logger.LogDebug(
                    "联系消息 {MessageId} 的邮件通知未入队（重复或队列已满），可由下次启动补偿扫描补发",
                    message.Id);
            }
        }
    }

    /// <summary>
    /// 服务关闭前清空队列中剩余的消息。
    /// </summary>
    private async Task DrainRemainingAsync()
    {
        try
        {
            var remaining = _queue.DequeueBatch(maxBatchSize: 10000);
            while (remaining.Count > 0)
            {
                await SaveBatchAsync(remaining, CancellationToken.None);
                remaining = _queue.DequeueBatch(maxBatchSize: 10000);
            }
            _logger.LogInformation("队列残留消息已全部刷入数据库");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "队列残留消息刷入数据库时发生异常");
        }
    }
}
