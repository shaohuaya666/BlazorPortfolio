using System.Net;
using System.Text;
using MimeKit;

namespace BlazorPortfolio.Services.Email;

/// <summary>
/// 联系表单通知邮件的内容构建器。<br/>
/// 统一负责主题、正文结构、Reply-To 以及防注入处理（CRLF 头注入 / HTML 注入），便于独立验证。
/// </summary>
public static class ContactEmailComposer
{
    /// <summary>主题中姓名部分的最大长度，避免超长主题被邮件服务端截断或拒收</summary>
    private const int MaxSubjectNameLength = 60;

    /// <summary>
    /// 构建一封联系通知邮件（同时提供纯文本与 HTML 两个正文版本）。
    /// </summary>
    public static MimeMessage Compose(EmailNotification notification, SmtpOptions options)
    {
        var senderName = Normalize(notification.SenderName, "匿名访客");
        var senderEmail = SanitizeHeader(notification.SenderEmail);
        var content = notification.Content ?? string.Empty;
        var submittedAt = notification.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
        var clientIp = string.IsNullOrWhiteSpace(notification.ClientIp) ? "未知" : notification.ClientIp;

        var message = new MimeMessage();

        // From 固定为系统发件账号，避免伪造；显示名做头注入清洗
        message.From.Add(new MailboxAddress(SanitizeHeader(options.FromDisplayName), options.FromAddress));
        message.To.Add(new MailboxAddress("站长", options.ToAddress));

        // Reply-To 指向提交者，站长直接回复即可回信
        if (MailboxAddress.TryParse(senderEmail, out var replyTo))
            message.ReplyTo.Add(replyTo);

        message.Subject = BuildSubject(senderName, notification.Timestamp);

        var builder = new BodyBuilder
        {
            TextBody = BuildTextBody(senderName, senderEmail, submittedAt, clientIp, content),
            HtmlBody = BuildHtmlBody(senderName, senderEmail, submittedAt, clientIp, content)
        };
        message.Body = builder.ToMessageBody();

        return message;
    }

    /// <summary>主题：【作品站联系】姓名 - 时间（便于收件箱检索）</summary>
    private static string BuildSubject(string senderName, DateTime timestamp)
    {
        var name = senderName.Length > MaxSubjectNameLength
            ? senderName[..MaxSubjectNameLength] + "…"
            : senderName;

        return $"【作品站联系】{name} - {timestamp:yyyy-MM-dd HH:mm}";
    }

    private static string BuildTextBody(
        string senderName, string senderEmail, string submittedAt, string clientIp, string content)
    {
        var sb = new StringBuilder();
        sb.AppendLine("【联系合作】收到新的表单提交");
        sb.AppendLine(new string('-', 32));
        sb.AppendLine($"姓名：{senderName}");
        sb.AppendLine($"邮箱：{senderEmail}");
        sb.AppendLine($"时间：{submittedAt}");
        sb.AppendLine($"来源 IP：{clientIp}");
        sb.AppendLine(new string('-', 32));
        sb.AppendLine("留言内容：");
        sb.AppendLine(content);
        sb.AppendLine(new string('-', 32));
        sb.AppendLine("提示：直接回复本邮件将发送至提交者邮箱。");
        return sb.ToString();
    }

    private static string BuildHtmlBody(
        string senderName, string senderEmail, string submittedAt, string clientIp, string content)
    {
        var nameHtml = WebUtility.HtmlEncode(senderName);
        var emailHtml = WebUtility.HtmlEncode(senderEmail);
        var timeHtml = WebUtility.HtmlEncode(submittedAt);
        var ipHtml = WebUtility.HtmlEncode(clientIp);
        var contentHtml = WebUtility.HtmlEncode(content).Replace("\r\n", "\n").Replace("\n", "<br/>");

        return $"""
            <div style="font-family:-apple-system,Segoe UI,Microsoft YaHei,sans-serif;font-size:14px;color:#1f2937;line-height:1.7">
              <h3 style="margin:0 0 12px">【联系合作】收到新的表单提交</h3>
              <table style="border-collapse:collapse;margin-bottom:12px">
                <tr><td style="padding:2px 12px 2px 0;color:#6b7280">姓名</td><td>{nameHtml}</td></tr>
                <tr><td style="padding:2px 12px 2px 0;color:#6b7280">邮箱</td><td>{emailHtml}</td></tr>
                <tr><td style="padding:2px 12px 2px 0;color:#6b7280">时间</td><td>{timeHtml}</td></tr>
                <tr><td style="padding:2px 12px 2px 0;color:#6b7280">来源 IP</td><td>{ipHtml}</td></tr>
              </table>
              <div style="padding:12px;background:#f3f4f6;border-radius:6px;white-space:normal">{contentHtml}</div>
              <p style="color:#6b7280;font-size:12px;margin-top:12px">提示：直接回复本邮件将发送至提交者邮箱。</p>
            </div>
            """;
    }

    /// <summary>去除 CR/LF 等头注入字符</summary>
    private static string SanitizeHeader(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.Replace("\r", string.Empty).Replace("\n", string.Empty).Trim();
    }

    /// <summary>规范化正文取值：清洗头注入字符并提供默认值</summary>
    private static string Normalize(string? value, string fallback)
    {
        var sanitized = SanitizeHeader(value);
        return string.IsNullOrEmpty(sanitized) ? fallback : sanitized;
    }
}
