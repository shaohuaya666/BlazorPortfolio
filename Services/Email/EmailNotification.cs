using BlazorPortfolio.Models;

namespace BlazorPortfolio.Services.Email;

/// <summary>
/// 待投递的邮件通知快照。<br/>
/// 只保留发信所需字段，避免后台服务持有 EF 实体引用。
/// </summary>
/// <param name="MessageId">ChatMessages 主键，用于回写发送状态与去重</param>
/// <param name="SenderName">提交者姓名</param>
/// <param name="SenderEmail">提交者邮箱（作为 Reply-To）</param>
/// <param name="Content">留言正文</param>
/// <param name="ClientIp">提交来源 IP</param>
/// <param name="Timestamp">提交时间</param>
public sealed record EmailNotification(
    int MessageId,
    string SenderName,
    string SenderEmail,
    string Content,
    string? ClientIp,
    DateTime Timestamp)
{
    /// <summary>
    /// 由已落库的联系消息构造通知快照。
    /// </summary>
    public static EmailNotification From(ChatMessage message) => new(
        message.Id,
        message.SenderName ?? string.Empty,
        message.SenderEmail ?? string.Empty,
        message.Content ?? string.Empty,
        message.ClientIp,
        message.Timestamp);
}
