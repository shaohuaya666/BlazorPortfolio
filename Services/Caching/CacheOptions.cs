using System.Text.Json.Serialization;

namespace BlazorPortfolio.Services.Caching;

/// <summary>
/// 缓存统一配置，对应 appsettings.json 的 "Cache" 节点。<br/>
/// 所有与缓存相关的策略（TTL、抖动、体积上限、锁、熔断）集中在此，避免散落在业务代码中。
/// </summary>
public class CacheOptions
{
    /// <summary>配置节点名称</summary>
    public const string SectionName = "Cache";

    /// <summary>
    /// 主缓存实现：<see cref="CacheProviderKind.Redis"/>（默认）或 <see cref="CacheProviderKind.Memory"/>。<br/>
    /// 设置为 Memory 时完全走本地缓存，等价于迁移前的行为，用于灰度切回。
    /// </summary>
    public CacheProviderKind Provider { get; set; } = CacheProviderKind.Redis;

    /// <summary>全局缓存键前缀，用于与其它系统共享同一 Redis 实例时隔离命名空间</summary>
    public string KeyPrefix { get; set; } = "BlazorPortfolio";

    /// <summary>缓存键版本号；数据结构变更需要全员失效时递增即可（如 v1 → v2）</summary>
    public string KeyVersion { get; set; } = "v1";

    /// <summary>默认过期时间（分钟），业务未显式指定时使用</summary>
    public int DefaultDurationMinutes { get; set; } = 30;

    /// <summary>
    /// Redis 不可用、降级到本地缓存时的过期时间上限（分钟）。<br/>
    /// 降级TTL 取 min(业务TTL, 本值)，避免 Redis 恢复后长时间读到过期数据。
    /// </summary>
    public int FallbackDurationMinutes { get; set; } = 5;

    /// <summary>过期时间随机抖动比例（0.1 表示 ±10%），用于打散同时过期，防止雪崩</summary>
    public double ExpirationJitterRatio { get; set; } = 0.1;

    /// <summary>抖动绝对值上限（分钟），避免长 TTL 键抖动过大</summary>
    public int MaxJitterMinutes { get; set; } = 10;

    /// <summary>
    /// 空值（factory 返回 null）缓存时间（分钟），用于防止缓存穿透。<br/>
    /// 小于等于 0 表示关闭空值缓存（保持迁移前行为）。
    /// </summary>
    public int NullValueDurationMinutes { get; set; } = 5;

    /// <summary>单条缓存值的最大体积（KB），超过则跳过写入并记录警告，防止大 Key 拖垮 Redis</summary>
    public int MaxValueSizeKb { get; set; } = 512;

    /// <summary>是否启用跨实例分布式锁（缓存击穿防护），关闭后仅使用进程内单飞</summary>
    public bool EnableDistributedLock { get; set; } = true;

    /// <summary>进程内单飞（同一进程同一 Key 只允许一个线程回源）等待超时（毫秒）</summary>
    public int SingleFlightWaitMs { get; set; } = 3000;

    /// <summary>分布式锁最长持有时间（毫秒），超时自动释放，防止持有者崩溃导致死锁</summary>
    public int LockExpiryMs { get; set; } = 5000;

    /// <summary>尝试获取分布式锁的最长等待时间（毫秒）</summary>
    public int LockAcquireWaitMs { get; set; } = 1000;

    /// <summary>未拿到分布式锁时，等待其它实例回填缓存的轮询间隔（毫秒）</summary>
    public int LockRetryDelayMs { get; set; } = 200;

    /// <summary>未拿到分布式锁时，等待其它实例回填缓存的轮询次数</summary>
    public int LockRetryCount { get; set; } = 5;

    /// <summary>连续失败次数达到阈值后触发熔断，期间直接走本地降级缓存</summary>
    public int FailureThreshold { get; set; } = 3;

    /// <summary>熔断持续时间（毫秒），到期后放行一次请求做半开探测</summary>
    public int BreakDurationMs { get; set; } = 30_000;

    /// <summary>Redis 专属配置</summary>
    public RedisCacheOptions Redis { get; set; } = new();
}

/// <summary>支持的缓存实现类型</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CacheProviderKind
{
    /// <summary>Redis（分布式缓存，跨实例共享）</summary>
    Redis,

    /// <summary>本地内存缓存（仅本进程可见，用于降级或回滚）</summary>
    Memory
}

/// <summary>Redis 连接相关配置</summary>
public class RedisCacheOptions
{
    /// <summary>
    /// 连接字符串，支持 StackExchange.Redis 语法。<br/>
    /// 例：127.0.0.1:6379,password=xxx,ssl=false,abortConnect=false
    /// </summary>
    public string ConnectionString { get; set; } = "127.0.0.1:6379";

    /// <summary>数据库编号（集群模式下被忽略）</summary>
    public int Database { get; set; }

    /// <summary>连接超时（毫秒）</summary>
    public int ConnectTimeoutMs { get; set; } = 3000;

    /// <summary>同步操作超时（毫秒）</summary>
    public int SyncTimeoutMs { get; set; } = 3000;

    /// <summary>初始连接失败时的重试次数</summary>
    public int ConnectRetry { get; set; } = 2;

    /// <summary>
    /// 初始连接失败时是否中止创建连接。<br/>
    /// 必须为 false：否则 Redis 未就绪会导致应用启动失败，违背"降级可用"目标。
    /// </summary>
    public bool AbortOnConnectFail { get; set; }

    /// <summary>是否启用 TLS</summary>
    public bool Ssl { get; set; }

    /// <summary>Redis 名称，便于服务端 CLIENT LIST 定位来源</summary>
    public string ClientName { get; set; } = "BlazorPortfolio";
}
