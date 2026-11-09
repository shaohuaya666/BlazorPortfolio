namespace BlazorPortfolio.Services.Email;

/// <summary>
/// 邮件发送抽象。<br/>
/// 默认由 SmtpEmailSender（MailKit + SMTP）实现；如需切换为阿里云邮件推送等服务，仅需替换实现。
/// </summary>
public interface IEmailSender
{
    /// <summary>配置是否完整可用。未配置时后台服务不启动，消息仅落库</summary>
    bool IsConfigured { get; }

    /// <summary>
    /// 发送联系表单通知邮件。失败时抛出异常，由调用方决定重试或标记失败。
    /// </summary>
    Task SendContactNotificationAsync(EmailNotification notification, CancellationToken ct = default);
}
