namespace BlazorPortfolio.Services.Caching;

/// <summary>
/// 统一缓存访问层接口（CacheProvider）。<br/>
/// 业务层（<see cref="CacheService"/>）只依赖该接口，Redis 与本地内存两种实现可互相替换，
/// 使上层代码与具体缓存中间件解耦。
/// </summary>
public interface ICacheProvider
{
    /// <summary>实现名称，用于日志区分（Redis / Memory）</summary>
    string Name { get; }

    /// <summary>当前实例是否可用（Redis 未配置或断连时为 false）</summary>
    bool IsAvailable { get; }

    /// <summary>是否支持跨实例分布式锁</summary>
    bool SupportsDistributedLock { get; }

    /// <summary>
    /// 读取缓存条目，未命中或被判定不可用时返回 null。
    /// </summary>
    Task<CacheEntry<T>?> GetAsync<T>(string cacheKey, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 写入缓存条目。ttl 非法时实现需使用自身默认 TTL，禁止写入永不过期的键。
    /// </summary>
    Task SetAsync<T>(string cacheKey, CacheEntry<T> entry, TimeSpan ttl, CancellationToken cancellationToken = default) where T : class;

    /// <summary>删除指定缓存条目</summary>
    Task RemoveAsync(string cacheKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// 尝试获取分布式锁（不可重入）。
    /// </summary>
    /// <param name="lockKey">锁键</param>
    /// <param name="expiry">锁最长持有时间，超时自动释放</param>
    /// <param name="wait">最长等待时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>获取成功返回可释放的锁句柄；超时未获取返回 null</returns>
    Task<IDisposable?> AcquireLockAsync(string lockKey, TimeSpan expiry, TimeSpan wait, CancellationToken cancellationToken = default);
}
