namespace BlazorPortfolio.Services.Realtime;

/// <summary>
/// 系统日志推送的限流配置，对应 appsettings.json 的 "HubLog" 节点。<br/>
/// 日志正文的长度上限固定为 <see cref="HubLogSanitizer.MaxContentLength"/>，不在此处开放配置，
/// 避免配置值超过 ChatMessages.Content 的列宽导致落库失败。
/// </summary>
public class HubLogOptions
{
    /// <summary>配置节点名称</summary>
    public const string SectionName = "HubLog";

    /// <summary>是否启用频率限制（关闭后仅保留校验与截断）</summary>
    public bool RateLimitEnabled { get; set; } = true;

    /// <summary>突发窗口时长（秒）</summary>
    public int BurstWindowSeconds { get; set; } = 5;

    /// <summary>突发窗口内允许的日志条数（需覆盖切换标签、连续点击等正常交互）</summary>
    public int MaxPerBurstWindow { get; set; } = 5;

    /// <summary>滑动窗口时长（秒）</summary>
    public int WindowSeconds { get; set; } = 30;

    /// <summary>滑动窗口内允许的日志条数</summary>
    public int MaxPerWindow { get; set; } = 20;

    /// <summary>连续被限流的次数阈值，达到后断开连接；小于等于 0 表示从不断开</summary>
    public int AbortAfterConsecutiveRejections { get; set; } = 10;
}
