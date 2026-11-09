using System.Net.Mail;
using BlazorPortfolio.Models;
using BlazorPortfolio.Services;
using BlazorPortfolio.Services.Contact;
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
    private readonly MessageQueueService _queue;
    private readonly ContactRateLimiter _contactRateLimiter;
    private readonly IOptions<ContactFormOptions> _contactOptions;
    private readonly ILogger<PortfolioHub> _logger;

    public PortfolioHub(
        MessageQueueService queue,
        ContactRateLimiter contactRateLimiter,
        IOptions<ContactFormOptions> contactOptions,
        ILogger<PortfolioHub> logger)
    {
        _queue = queue;
        _contactRateLimiter = contactRateLimiter;
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

        // 广播给所有连接的客户端
        await Clients.All.SendAsync("ReceiveContactLog", logMessage);

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
    /// 广播系统日志到所有连接的客户端
    /// </summary>
    public async Task BroadcastSystemLog(string log)
    {
        var now = DateTime.Now;
        var timestamp = now.ToString("HH:mm:ss");

        await Clients.All.SendAsync("ReceiveSystemLog", $"[{timestamp}] {log}");

        // 入队列持久化
        await _queue.EnqueueAsync(new ChatMessage
        {
            MessageType = "SystemLog",
            Content = log,
            ConnectionId = Context.ConnectionId,
            Timestamp = now
        });
    }

    /// <summary>
    /// 客户端连接时触发：广播上线通知 + 记录连接事件
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var now = DateTime.Now;
        var timestamp = now.ToString("HH:mm:ss");

        await Clients.All.SendAsync("ReceiveSystemLog",
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

        await Clients.All.SendAsync("ReceiveSystemLog", $"[{timestamp}] 用户已断开连接");
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
