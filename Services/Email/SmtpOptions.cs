namespace BlazorPortfolio.Services.Email;

/// <summary>
/// 邮件通知配置，对应 appsettings.json 的 "Smtp" 节点。<br/>
/// 敏感项（Password）只允许出现在服务器端 appsettings.Production.json 或环境变量（Smtp__Password）中。
/// </summary>
public class SmtpOptions
{
    /// <summary>配置节点名称</summary>
    public const string SectionName = "Smtp";

    /// <summary>是否启用邮件通知。为 false 时不建立邮件队列、不发信，消息仅落库</summary>
    public bool Enabled { get; set; }

    /// <summary>SMTP 服务器地址（163 邮箱为 smtp.163.com）</summary>
    public string Host { get; set; } = "smtp.163.com";

    /// <summary>SMTP 端口。SSL 直连用 465，STARTTLS 用 587</summary>
    public int Port { get; set; } = 465;

    /// <summary>true 使用 STARTTLS 升级（587），false 使用 SSL 直连（465）</summary>
    public bool UseStartTls { get; set; }

    /// <summary>登录 SMTP 的账号（通常与发件地址一致）</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>SMTP 授权码（不是邮箱登录密码），敏感项</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>发件地址，必须与 SMTP 账号一致，否则会被服务端拒信</summary>
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>发件人显示名</summary>
    public string FromDisplayName { get; set; } = "作品集联系表单";

    /// <summary>收件地址（站长邮箱），固定从配置读取，不接受用户输入</summary>
    public string ToAddress { get; set; } = "shaohuaya520@163.com";

    /// <summary>单次 SMTP 操作超时（秒）</summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>发送失败最大重试次数（不含首次发送）</summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>重试退避间隔（秒），按次序取用；次数超出数组长度时沿用最后一个值</summary>
    public int[] RetryDelaySeconds { get; set; } = [60, 300, 900];

    /// <summary>启动补偿扫描的回溯天数（只补发该时间范围内的未发邮件）</summary>
    public int CompensationLookbackDays { get; set; } = 7;

    /// <summary>启动补偿扫描的单次最大条数</summary>
    public int CompensationBatchSize { get; set; } = 50;

    /// <summary>配置是否完整可用（缺任意关键项则视为未配置，服务只记日志不发送）</summary>
    public bool IsConfigured =>
        Enabled
        && !string.IsNullOrWhiteSpace(Host)
        && Port > 0
        && !string.IsNullOrWhiteSpace(UserName)
        && !string.IsNullOrWhiteSpace(Password)
        && !string.IsNullOrWhiteSpace(FromAddress)
        && !string.IsNullOrWhiteSpace(ToAddress);
}
