namespace BlazorPortfolio.Services.Contact;

/// <summary>
/// 联系表单的校验与限流配置，对应 appsettings.json 的 "ContactForm" 节点。
/// </summary>
public class ContactFormOptions
{
    /// <summary>配置节点名称</summary>
    public const string SectionName = "ContactForm";

    /// <summary>是否启用频率限制（关闭后仅保留输入校验）</summary>
    public bool RateLimitEnabled { get; set; } = true;

    /// <summary>滑动窗口时长（分钟）</summary>
    public int PerIpWindowMinutes { get; set; } = 10;

    /// <summary>单个 IP 在滑动窗口内允许的提交次数</summary>
    public int PerIpMaxPerWindow { get; set; } = 3;

    /// <summary>单个 IP 每日允许的提交次数</summary>
    public int PerIpMaxPerDay { get; set; } = 20;

    /// <summary>同一 IP 两次提交的最小间隔（秒）</summary>
    public int MinSubmitIntervalSeconds { get; set; } = 30;

    /// <summary>姓名最大长度（与 ChatMessages.SenderName 列宽对齐）</summary>
    public int MaxNameLength { get; set; } = 200;

    /// <summary>邮箱最大长度（与 ChatMessages.SenderEmail 列宽对齐）</summary>
    public int MaxEmailLength { get; set; } = 200;

    /// <summary>留言最大长度（与 ChatMessages.Content 列宽对齐）</summary>
    public int MaxMessageLength { get; set; } = 2000;
}
