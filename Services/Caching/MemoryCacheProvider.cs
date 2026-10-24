using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace BlazorPortfolio.Services.Caching;

/// <summary>
/// 本地内存缓存实现（<see cref="IMemoryCache"/>），用于：<br/>
/// 1. Redis 故障或被熔断时的降级（CAP 取舍：牺牲一致性保可用）；<br/>
/// 2. 未部署 Redis 的环境（本地开发）以及需要快速回滚的场景。<br/>
/// 注意：键值对不跨实例共享，多个实例间可能出现短暂不一致，因此 TTL 由上层限制在较低区间。
/// </summary>
public sealed class MemoryCacheProvider : ICacheProvider
{
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public MemoryCacheProvider(IMemoryCache cache)
    {
        _cache = cache;
    }

    /// <inheritdoc />
    public string Name => "Memory";

    /// <inheritdoc />
    public bool IsAvailable => true;

    /// <inheritdoc />
    public bool SupportsDistributedLock => false;

    /// <inheritdoc />
    public Task<CacheEntry<T>?> GetAsync<T>(string cacheKey, CancellationToken cancellationToken = default)
        where T : class
    {
        _cache.TryGetValue(cacheKey, out CacheEntry<T>? entry);
        return Task.FromResult(entry);
    }

    /// <inheritdoc />
    public Task SetAsync<T>(string cacheKey, CacheEntry<T> entry, TimeSpan ttl,
        CancellationToken cancellationToken = default) where T : class
    {
        if (ttl <= TimeSpan.Zero)
        {
            _cache.Remove(cacheKey);
            return Task.CompletedTask;
        }

        // 本地缓存无容量上限控制字段，这里依赖 IMemoryCache 的 SizeLimit 与滑动过期策略
        _cache.Set(cacheKey, entry, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        });

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        _cache.Remove(cacheKey);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IDisposable?> AcquireLockAsync(string lockKey, TimeSpan expiry, TimeSpan wait,
        CancellationToken cancellationToken = default)
    {
        var gate = _locks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));

        if (await gate.WaitAsync(wait, cancellationToken).ConfigureAwait(false))
        {
            return new LocalLock(gate);
        }

        return null;
    }

    /// <summary>进程内锁句柄</summary>
    private sealed class LocalLock : IDisposable
    {
        private readonly SemaphoreSlim _gate;
        private bool _released;

        public LocalLock(SemaphoreSlim gate)
        {
            _gate = gate;
        }

        public void Dispose()
        {
            if (_released)
            {
                return;
            }

            _released = true;
            _gate.Release();
        }
    }
}
