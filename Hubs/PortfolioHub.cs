using System.Net.Mail;
using BlazorPortfolio.Models;
using BlazorPortfolio.Services;
using BlazorPortfolio.Services.Contact;
using BlazorPortfolio.Services.Realtime;
using BlazorPortfolio.Services.VisitTracking;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace BlazorPortfolio.Hubs;

/// <summary>
/// 作品集 SignalR Hub，负责实时双向通信：
/// 联系表单消息广播、系统日志推送、遥测数据同步。
/// 所有交互消息先入内存队列，由后台定时任务批量持久化到数据库。
/// </summary>
public class PortfolioHub : Hub
{
    /// <summary>
    /// 实时日志订阅组：只有显式订阅的连接才会收到系统日志与联系表单日志，
    /// 避免任一访客的操作触发全站页面重渲染。
    /// </summary>
    public const string SystemLogGroup = "system-log";

    private readonly MessageQueueService _queue;
    private readonly ContactRateLimiter _contactRateLimiter;
    private readonly HubLogRateLimiter _logRateLimiter;
    private readonly IOptions<ContactFormOptions> _contactOptions;
    private readonly ILogger<PortfolioHub> _logger;

    public PortfolioHub(
        MessageQueueService queue,
        ContactRateLimiter contactRateLimiter,
        HubLogRateLimiter logRateLimiter,
        IOptions<ContactFormOptions> contactOptions,
        ILogger<PortfolioHub> logger)
    {
        _queue = queue;
        _contactRateLimiter = contactRateLimiter;
        _logRateLimiter = logRateLimiter;
        _contactOptions = contactOptions;
        _logger = logger;
    }

    /// <summary>
    /// 发送联系表单消息：先校验与限流，入队成功后再广播日志并确认给调用方。
    /// 校验失败或触发限流时抛出 <see cref="HubException"/>，调用方不会收到成功提示。
    /// </summary>
    public async Task SendContactMessage(string name, string email, string message)
    {
        var normalizedName = (name ?? string.Empty).Trim();
        var normalizedEmail = (email ?? string.Empty).Trim();
        var normalizedMessage = (message ?? string.Empty).Trim();

        if (!TryValidateContact(normalizedName, normalizedEmail, normalizedMessage, out var error))
        {
            _logger.LogWarning("联系表单提交被拒绝：{Reason}", error);
            throw new HubException(error);
        }

        var clientIp = ResolveClientIp();
        if (!_contactRateLimiter.TryAcquire(clientIp))
            throw new HubException("提交过于频繁，请稍后再试。");

        var now = DateTime.Now;
        var timestamp = now.ToString("HH:mm:ss");
        var logMessage = $"[{timestamp}] PACKET_TRANSMISSION: Handshake from {normalizedName} ({normalizedEmail})";

        // 入队列持久化；落库成功后由后台服务转发为邮件通知
        await _queue.EnqueueAsync(new ChatMessage
        {
            MessageType = ChatMessage.ContactMessageType,
            SenderName = normalizedName,
            SenderEmail = normalizedEmail,
            Content = normalizedMessage,
            ConnectionId = Context.ConnectionId,
            ClientIp = clientIp,
            Timestamp = now,
            EmailStatus = ChatMessage.EmailStatusPending
        });

        // 仅推送给已订阅实时日志的客户端；提交者本人另行收到 ContactSubmitSuccess
        await Clients.Group(SystemLogGroup).SendAsync("ReceiveContactLog", logMessage);

        // 发送成功消息给当前客户端
        await Clients.Caller.SendAsync("ContactSubmitSuccess", $"消息已通过 SignalR 实时传输！来自 {normalizedName}");
    }

    /// <summary>
    /// 校验联系表单入参：非空、长度与数据库列宽对齐、邮箱格式合法。
    /// </summary>
    private bool TryValidateContact(string name, string email, string message, out string error)
    {
        var options = _contactOptions.Value;

        if (string.IsNullOrEmpty(name))
        {
            error = "请填写姓名。";
            return false;
        }

        if (name.Length > options.MaxNameLength)
        {
            error = $"姓名不能超过 {options.MaxNameLength} 个字符。";
            return false;
        }

        if (string.IsNullOrEmpty(email) || email.Length > options.MaxEmailLength)
        {
            error = $"邮箱不能为空且不能超过 {options.MaxEmailLength} 个字符。";
            return false;
        }

        if (!MailAddress.TryCreate(email, out _))
        {
            error = "邮箱格式不正确。";
            return false;
        }

        if (string.IsNullOrEmpty(message))
        {
            error = "请填写留言内容。";
            return false;
        }

        if (message.Length > options.MaxMessageLength)
        {
            error = $"留言不能超过 {options.MaxMessageLength} 个字符。";
            return false;
        }

        error = string.Empty;
        return true;
    }

    /// <summary>
    /// 推送系统日志给已订阅的客户端（见 <see cref="SubscribeSystemLog"/>）。<br/>
    /// 入参依次经过有效性校验、频率限制与归一化截断：空白日志直接丢弃，
    /// 超限静默丢弃（连续超限则断开连接），正文压到 <see cref="HubLogSanitizer.MaxContentLength"/>
    /// 以内以避免超长入库导致整批消息落库失败。
    /// </summary>
    public async Task BroadcastSystemLog(string log)
    {
        // 空白日志无意义，且不应消耗限流额度（调用方为 fire-and-forget，故此处不抛异常）
        if (string.IsNullOrWhiteSpace(log))
            return;

        if (!TryAcquireHubQuota(HubLogRateLimitScope.Log))
            return;

        // 归一化（折叠换行、剔除控制字符）并截断，防止伪造日志行与超长入库
        var content = HubLogSanitizer.Normalize(log);
        if (content is null)
            return;

        var now = DateTime.Now;
        var line = $"[{now:HH:mm:ss}] {content}";

        // 订阅者（不含自己）收到该日志；发送方单独回显一条，
        // 保留本地交互反馈，且不会触发其他未订阅页面的重渲染
        await Clients.OthersInGroup(SystemLogGroup).SendAsync("ReceiveSystemLog", line);
        await Clients.Caller.SendAsync("ReceiveSystemLog", line);

        // 入队列持久化（Content 已保证不超过 ChatMessages.Content 的列宽）
        await _queue.EnqueueAsync(new ChatMessage
        {
            MessageType = "SystemLog",
            Content = content,
            ConnectionId = Context.ConnectionId,
            Timestamp = now
        });
    }

    /// <summary>
    /// 订阅实时日志：订阅后该连接才会收到系统日志与联系表单日志推送。<br/>
    /// 由页面上的「实时日志」开关调用；订阅/退订动作同样受限流保护。
    /// </summary>
    public async Task SubscribeSystemLog()
    {
        if (!TryAcquireHubQuota(HubLogRateLimitScope.Subscription))
            return;

        await Groups.AddToGroupAsync(Context.ConnectionId, SystemLogGroup);
    }

    /// <summary>
    /// 取消实时日志订阅：退订后不再收到日志推送，页面不再因他人操作重渲染。
    /// </summary>
    public async Task UnsubscribeSystemLog()
    {
        if (!TryAcquireHubQuota(HubLogRateLimitScope.Subscription))
            return;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, SystemLogGroup);
    }

    /// <summary>
    /// 客户端连接时触发：广播上线通知 + 记录连接事件
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var now = DateTime.Now;
        var timestamp = now.ToString("HH:mm:ss");

        await Clients.Group(SystemLogGroup).SendAsync("ReceiveSystemLog",
            $"[{timestamp}] 新用户已连接 SignalR Hub (Connection ID: {Context.ConnectionId[..Math.Min(8, Context.ConnectionId.Length)]}...)");
        await base.OnConnectedAsync();

        // 入队列持久化（附带真实客户端 IP，便于定位异常连接来源）
        await _queue.EnqueueAsync(new ChatMessage
        {
            MessageType = "Connection",
            Content = $"用户已连接",
            ConnectionId = Context.ConnectionId,
            ClientIp = ResolveClientIp(),
            Timestamp = now
        });
    }

    /// <summary>
    /// 客户端断开连接时触发：广播离线通知 + 记录断开事件
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var now = DateTime.Now;
        var timestamp = now.ToString("HH:mm:ss");

        await Clients.Group(SystemLogGroup).SendAsync("ReceiveSystemLog", $"[{timestamp}] 用户已断开连接");
        await base.OnDisconnectedAsync(exception);

        // 入队列持久化（附带真实客户端 IP，便于定位异常连接来源）
        await _queue.EnqueueAsync(new ChatMessage
        {
            MessageType = "Disconnection",
            Content = exception?.Message ?? "正常断开",
            ConnectionId = Context.ConnectionId,
            ClientIp = ResolveClientIp(),
            Timestamp = now
        });
    }

    /// <summary>
    /// 模拟实时遥测数据推送（高频消息，不持久化）
    /// </summary>
    public async Task UpdateTelemetry(int hz)
    {
        await Clients.All.SendAsync("ReceiveTelemetry", hz);
    }

    /// <summary>
    /// 按「客户端 IP + 连接」占用一次配额，用于日志推送与订阅动作的限流。
    /// </summary>
    /// <returns>允许继续执行返回 true；超限或已断开连接返回 false</returns>
    private bool TryAcquireHubQuota(HubLogRateLimitScope scope)
    {
        var limitKey = $"{ResolveClientIp() ?? "unknown"}|{Context.ConnectionId}";
        switch (_logRateLimiter.TryAcquire(limitKey, scope))
        {
            case HubLogRateLimitDecision.Throttled:
                return false;
            case HubLogRateLimitDecision.Abort:
                _logger.LogWarning("Hub 调用持续超限，断开连接 {ConnectionId}（{Scope}）", Context.ConnectionId, scope);
                Context.Abort();
                return false;
            default:
                return true;
        }
    }

    /// <summary>
    /// 从 Hub 的握手上下文中取客户端 IP；ForwardedHeaders 已在管道最前完成代理还原。
    /// 取不到时返回 null，不影响原有连接日志写入。
    /// </summary>
    private string? ResolveClientIp()
    {
        try
        {
            return ClientIpResolver.Format(Context.GetHttpContext()?.Connection.RemoteIpAddress);
        }
        catch
        {
            return null;
        }
    }
}
