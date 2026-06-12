using Microsoft.AspNetCore.SignalR;

namespace BlazorPortfolio.Hubs;

public class PortfolioHub : Hub
{
    /// <summary>
    /// 发送联系表单消息
    /// </summary>
    public async Task SendContactMessage(string name, string email, string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var logMessage = $"[{timestamp}] PACKET_TRANSMISSION: Handshake from {name} ({email})";

        // 广播给所有连接的客户端
        await Clients.All.SendAsync("ReceiveContactLog", logMessage);

        // 发送成功消息给当前客户端
        await Clients.Caller.SendAsync("ContactSubmitSuccess", $"消息已通过 SignalR 实时传输！来自 {name}");
    }

    /// <summary>
    /// 广播系统日志
    /// </summary>
    public async Task BroadcastSystemLog(string log)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        await Clients.All.SendAsync("ReceiveSystemLog", $"[{timestamp}] {log}");
    }

    /// <summary>
    /// 客户端连接时触发
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        await Clients.All.SendAsync("ReceiveSystemLog", $"[{timestamp}] 新用户已连接 SignalR Hub (Connection ID: {Context.ConnectionId.Substring(0, 8)}...)");
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// 客户端断开连接时触发
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        await Clients.All.SendAsync("ReceiveSystemLog", $"[{timestamp}] 用户已断开连接");
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// 模拟实时遥测数据推送
    /// </summary>
    public async Task UpdateTelemetry(int hz)
    {
        await Clients.All.SendAsync("ReceiveTelemetry", hz);
    }
}
