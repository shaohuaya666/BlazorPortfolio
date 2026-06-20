using BlazorPortfolio.Models;
using BlazorPortfolio.Services;
using Microsoft.AspNetCore.SignalR;

namespace BlazorPortfolio.Hubs;

/// <summary>
/// 作品集 SignalR Hub，负责实时双向通信：
/// 联系表单消息广播、系统日志推送、遥测数据同步。
/// 所有交互消息先入内存队列，由后台定时任务批量持久化到数据库。
/// </summary>
public class PortfolioHub : Hub
{
    private readonly MessageQueueService _queue;

    public PortfolioHub(MessageQueueService queue)
    {
        _queue = queue;
    }

    /// <summary>
    /// 发送联系表单消息，广播给所有连接客户端并返回成功确认给调用方
    /// </summary>
    public async Task SendContactMessage(string name, string email, string message)
    {
        var now = DateTime.Now;
        var timestamp = now.ToString("HH:mm:ss");
        var logMessage = $"[{timestamp}] PACKET_TRANSMISSION: Handshake from {name} ({email})";

        // 广播给所有连接的客户端
        await Clients.All.SendAsync("ReceiveContactLog", logMessage);

        // 发送成功消息给当前客户端
        await Clients.Caller.SendAsync("ContactSubmitSuccess", $"消息已通过 SignalR 实时传输！来自 {name}");

        // 入队列持久化
        await _queue.EnqueueAsync(new ChatMessage
        {
            MessageType = "Contact",
            SenderName = name,
            SenderEmail = email,
            Content = message,
            ConnectionId = Context.ConnectionId,
            Timestamp = now
        });
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

        // 入队列持久化
        await _queue.EnqueueAsync(new ChatMessage
        {
            MessageType = "Connection",
            Content = $"用户已连接",
            ConnectionId = Context.ConnectionId,
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

        // 入队列持久化
        await _queue.EnqueueAsync(new ChatMessage
        {
            MessageType = "Disconnection",
            Content = exception?.Message ?? "正常断开",
            ConnectionId = Context.ConnectionId,
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
}
