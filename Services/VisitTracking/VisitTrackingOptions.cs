namespace BlazorPortfolio.Services.VisitTracking;

/// <summary>
/// 访问来源采集配置，对应 appsettings.json 的 "VisitTracking" 节点。<br/>
/// 所有与 IP 采集、代理信任、数据留存相关的策略集中在此，避免散落在业务代码中。
/// </summary>
public class VisitTrackingOptions
{
    /// <summary>配置节点名称</summary>
    public const string SectionName = "VisitTracking";

    /// <summary>是否启用访问来源采集（关闭后不再写入 PageView 记录，已存数据不受影响）</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>是否记录页面访问（MessageType = PageView）；关闭后仅保留 Hub 连接事件的 IP</summary>
    public bool RecordPageViews { get; set; } = true;

    /// <summary>访问记录保留天数（仅清理 PageView/Connection/Disconnection/SystemLog，Contact 业务消息不清理）</summary>
    public int RetentionDays { get; set; } = 90;

    /// <summary>清理任务执行间隔（小时）</summary>
    public int CleanupIntervalHours { get; set; } = 24;

    /// <summary>清理时单批删除条数，避免一次删除过多导致长时间锁表</summary>
    public int CleanupBatchSize { get; set; } = 1000;

    /// <summary>单轮清理最多执行的批次数，防止高峰时段长时间占用数据库连接</summary>
    public int MaxCleanupBatchesPerRound { get; set; } = 10;

    /// <summary>
    /// 信任的代理跳数，用于限制 X-Forwarded-For 的解析深度。<br/>
    /// 单层 nginx 反代填 1 或 2 即可，过大值会放宽伪造空间。
    /// </summary>
    public int ForwardedHeadersLimit { get; set; } = 2;

    /// <summary>
    /// 受信任的代理地址列表（IP 或 IP:端口），只有来自这些地址的转发头才会被采纳。<br/>
    /// 默认仅信任本机回环地址；部署到 nginx 同机时保持默认即可。
    /// </summary>
    public string[] TrustedProxies { get; set; } = ["127.0.0.1", "::1"];

    /// <summary>
    /// 真实 IP 头名称（如 "CF-Connecting-IP"），默认关闭。<br/>
    /// 仅在明确接入 CDN 且确认该头由可信边缘节点写入时启用。
    /// </summary>
    public string? RealIpHeader { get; set; }
}
