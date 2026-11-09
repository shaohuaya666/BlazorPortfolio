using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;

namespace BlazorPortfolio.Services.Email;

/// <summary>
/// 基于 MailKit 的 SMTP 邮件发送实现。<br/>
/// 采用「每次发送建立独立连接」的方式，避免长时间空闲连接被邮件服务端断开后出现假连接状态；
/// 联系表单量级极小，连接开销可忽略。
/// </summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly IOptions<SmtpOptions> _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsConfigured => _options.Value.IsConfigured;

    /// <inheritdoc />
    public async Task SendContactNotificationAsync(EmailNotification notification, CancellationToken ct = default)
    {
        var options = _options.Value;
        if (!options.IsConfigured)
            throw new InvalidOperationException("Smtp 配置不完整，无法发送邮件通知。");

        using var message = ContactEmailComposer.Compose(notification, options);

        // 用取消令牌实现整体超时（MailKit 4.x 推荐做法，替代已过时的 Timeout 属性）
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(5, options.TimeoutSeconds)));

        var socketOptions = options.UseStartTls
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.SslOnConnect;

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(options.Host, options.Port, socketOptions, timeoutCts.Token);
            await client.AuthenticateAsync(options.UserName, options.Password, timeoutCts.Token);
            await client.SendAsync(message, timeoutCts.Token);
            await client.DisconnectAsync(quit: true, timeoutCts.Token);

            _logger.LogDebug("SMTP 投递完成：{Host}:{Port} -> {ToAddress}", options.Host, options.Port, options.ToAddress);
        }
        finally
        {
            if (client.IsConnected)
            {
                try
                {
                    await client.DisconnectAsync(quit: false, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "断开 SMTP 连接时出现异常（已忽略）");
                }
            }
        }
    }
}
