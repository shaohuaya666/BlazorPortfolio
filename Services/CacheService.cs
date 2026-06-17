using BlazorPortfolio.Attributes;
using Microsoft.Extensions.Caching.Memory;

namespace BlazorPortfolio.Services;

/// <summary>
/// 通用本地缓存服务，封装 Cache-Aside 模式。<br/>
/// 提供 <see cref="GetOrSetAsync{T}"/> 方法，自动处理"先查缓存、未命中则查库并回填"的逻辑。
/// </summary>
/// <remarks>
/// 用法：在需要缓存的方法中调用 <c>await _cacheService.GetOrSetAsync("Key", async () => await QueryDbAsync())</c>
/// 配合接口上的 <see cref="CachedAttribute"/> 特性标注缓存 Key 和过期时间。
/// </remarks>
public class CacheService
{
    private readonly IMemoryCache _cache;

    private const int DefaultDurationMinutes = 30;

    public CacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    /// <summary>
    /// 通过缓存 Key 获取数据，若缓存未命中则执行 <paramref name="factory"/> 并回填缓存。
    /// </summary>
    /// <typeparam name="T">缓存数据类型</typeparam>
    /// <param name="key">缓存 Key，建议与接口 <see cref="CachedAttribute.Key"/> 对应</param>
    /// <param name="factory">缓存未命中时的数据获取工厂（通常是数据库查询）</param>
    /// <param name="durationMinutes">缓存过期时间（分钟），默认 30 分钟</param>
    /// <returns>缓存或数据库查询结果</returns>
    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, int durationMinutes = DefaultDurationMinutes)
        where T : class
    {
        if (_cache.TryGetValue(key, out T? cached) && cached is not null)
        {
            return cached;
        }

        var result = await factory();

        if (result is not null)
        {
            _cache.Set(key, result, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(durationMinutes)
            });
        }

        // factory 约定返回非 null，这里通过 null-forgiving 抑制编译器警告
        return result!;
    }

    /// <summary>
    /// 主动移除指定 Key 的缓存项（用于数据更新后刷新缓存）
    /// </summary>
    /// <param name="key">缓存 Key</param>
    public void Remove(string key)
    {
        _cache.Remove(key);
    }
}
