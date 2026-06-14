using Microsoft.AspNetCore.SignalR;

namespace BlazorPortfolio.Hubs;

/// <summary>
/// 作品集 SignalR Hub，负责实时双向通信：
/// 联系表单消息广播、系统日志推送、遥测数据同步
/// </summary>
public class PortfolioHub : Hub
{
    /// <summary>
    /// 发送联系表单消息，广播给所有连接客户端并返回成功确认给调用方
    /// </summary>
    /// <param name="name">联系人姓名</param>
    /// <param name="email">联系人邮箱</param>
    /// <param name="message">消息内容</param>
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
    /// 广播系统日志到所有连接的客户端
    /// </summary>
    /// <param name="log">日志内容</param>
    public async Task BroadcastSystemLog(string log)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        await Clients.All.SendAsync("ReceiveSystemLog", $"[{timestamp}] {log}");
    }

    /// <summary>
    /// 客户端连接时触发：广播上线通知
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        await Clients.All.SendAsync("ReceiveSystemLog", $"[{timestamp}] 新用户已连接 SignalR Hub (Connection ID: {Context.ConnectionId.Substring(0, 8)}...)");
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// 客户端断开连接时触发：广播离线通知
    /// </summary>
    /// <param name="exception">断开异常（如有）</param>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        await Clients.All.SendAsync("ReceiveSystemLog", $"[{timestamp}] 用户已断开连接");
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// 模拟实时遥测数据推送，将频率广播给所有客户端
    /// </summary>
    /// <param name="hz">遥测频率（Hz）</param>
    public async Task UpdateTelemetry(int hz)
    {
        await Clients.All.SendAsync("ReceiveTelemetry", hz);
    }
}
