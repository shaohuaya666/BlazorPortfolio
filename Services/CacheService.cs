using System.Collections.Concurrent;
using System.Net.Sockets;
using BlazorPortfolio.Attributes;
using BlazorPortfolio.Services.Caching;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace BlazorPortfolio.Services;

/// <summary>
/// 通用缓存服务（业务门面），封装 Cache-Aside 模式并负责高可用策略。<br/>
/// 对上层暴露的方法签名保持不变：<see cref="GetOrSetAsync{T}"/>、<see cref="Remove"/>、<see cref="RemoveAsync"/>，
/// 因此调用方（如 <c>PortfolioService</c>）无需任何改动。
/// </summary>
/// <remarks>
/// 职责分工：
/// <list type="bullet">
/// <item><description><see cref="ICacheProvider"/>：只负责"怎么存/怎么取"，Redis 与本地内存两种实现可插拔。</description></item>
/// <item><description>本类：负责"存多久、键怎么命、并发怎么控、挂了怎么降级"，与具体中间件无关。</description></item>
/// </list>
/// 用法：在需要缓存的方法中调用 <c>await _cacheService.GetOrSetAsync("Key", async () => await QueryDbAsync())</c>
/// 配合接口上的 <see cref="CachedAttribute"/> 特性标注缓存 Key 和过期时间。
/// </remarks>
public class CacheService
{
    private readonly ICacheProvider _redis;
    private readonly ICacheProvider _memory;
    private readonly CacheOptions _options;
    private readonly ILogger<CacheService> _logger;

    /// <summary>进程内单飞门闩：同一进程同一 Key 只允许一个线程回源</summary>
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _singleFlight = new();

    private const int DefaultDurationMinutes = 30;

    // 熔断器状态（无第三方依赖的轻量实现）
    private int _failureCount;
    private long _circuitOpenUntilUnixMs;

    public CacheService(
        RedisCacheProvider redisProvider,
        MemoryCacheProvider memoryProvider,
        IOptions<CacheOptions> options,
        ILogger<CacheService> logger)
    {
        _redis = redisProvider;
        _memory = memoryProvider;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// 通过缓存 Key 获取数据，若缓存未命中则执行 <paramref name="factory"/> 并回填缓存。<br/>
    /// 内部依次处理：缓存读取 → 进程内单飞 → 分布式锁 → 回源 → 回填。
    /// </summary>
    /// <typeparam name="T">缓存数据类型</typeparam>
    /// <param name="key">缓存 Key，建议与接口 <see cref="CachedAttribute.Key"/> 对应</param>
    /// <param name="factory">缓存未命中时的数据获取工厂（通常是数据库查询）</param>
    /// <param name="durationMinutes">缓存过期时间（分钟），默认 30 分钟</param>
    /// <returns>缓存或数据库查询结果</returns>
    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, int durationMinutes = DefaultDurationMinutes)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(factory);

        var cacheKey = CacheKeys.Build(_options.KeyPrefix, _options.KeyVersion, key);
        var provider = SelectProvider();

        // 1) 命中直接返回（含"空值哨兵"，用于防穿透）
        var cached = await ExecuteAsync(p => p.GetAsync<T>(cacheKey), (CacheEntry<T>?)null, provider, "Get", cacheKey)
            .ConfigureAwait(false);
        if (cached is not null)
        {
            _logger.LogDebug("[缓存] 命中 {Key} ← {Provider}。", cacheKey, provider.Name);
            return cached.Value!;
        }

        // 2) 进程内单飞：避免单实例内大量并发请求同时回源（缓存击穿第一层防护）
        var gate = _singleFlight.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
        if (!await gate.WaitAsync(TimeSpan.FromMilliseconds(Math.Max(0, _options.SingleFlightWaitMs))).ConfigureAwait(false))
        {
            // 等待超时：可用性优先，直接回源，不参与回填
            _logger.LogDebug("[缓存] 键 {Key} 单飞等待超时，直接回源。", cacheKey);
            return await factory().ConfigureAwait(false);
        }

        try
        {
            // 3) 双重检查：等待期间可能已被其它线程回填
            provider = SelectProvider();
            cached = await ExecuteAsync(p => p.GetAsync<T>(cacheKey), (CacheEntry<T>?)null, provider, "Get", cacheKey)
                .ConfigureAwait(false);
            if (cached is not null)
            {
                return cached.Value!;
            }

            // 4) 分布式锁：跨实例仅允许一个实例回源（缓存击穿第二层防护）。
            //    本地降级实现不支持分布式锁，此时并发安全由上面的进程内单飞保证，直接走回源回填流程。
            var useDistributedLock = _options.EnableDistributedLock && provider.SupportsDistributedLock;
            IDisposable? distributedLock = useDistributedLock
                ? await TryAcquireDistributedLockAsync(provider, cacheKey).ConfigureAwait(false)
                : null;

            if (useDistributedLock && distributedLock is null)
            {
                // 抢锁失败：说明其它实例正在构建，短轮询等待其结果，避免重复回源
                for (var i = 0; i < Math.Max(0, _options.LockRetryCount); i++)
                {
                    await Task.Delay(_options.LockRetryDelayMs).ConfigureAwait(false);

                    cached = await ExecuteAsync(p => p.GetAsync<T>(cacheKey), (CacheEntry<T>?)null, provider, "Get", cacheKey)
                        .ConfigureAwait(false);
                    if (cached is not null)
                    {
                        return cached.Value!;
                    }
                }

                // 等待超时未得到回填数据：由本实例回源并回填（后写覆盖，TTL 较短，可接受）
                _logger.LogDebug("[缓存] 键 {Key} 未获取到分布式锁且等待回填超时，由本实例回源。", cacheKey);
            }

            try
            {
                cached = await ExecuteAsync(p => p.GetAsync<T>(cacheKey), (CacheEntry<T>?)null, provider, "Get", cacheKey)
                    .ConfigureAwait(false);
                if (cached is not null)
                {
                    return cached.Value!;
                }

                var result = await factory().ConfigureAwait(false);

                // 无论是否持有分布式锁都要回填，否则降级场景会退化为"每次都回源"
                await SetWithFallbackAsync(cacheKey, result, durationMinutes).ConfigureAwait(false);

                // factory 约定返回非 null，这里通过 null-forgiving 抑制编译器警告
                return result!;
            }
            finally
            {
                distributedLock?.Dispose();
            }
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>
    /// 主动移除指定 Key 的缓存项（用于数据更新后刷新缓存），同步版本，保留兼容。
    /// </summary>
    /// <param name="key">缓存 Key</param>
    public void Remove(string key)
    {
        RemoveAsync(key).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 主动移除指定 Key 的缓存项（推荐），Redis 与本地降级缓存同时失效。
    /// </summary>
    /// <param name="key">缓存 Key</param>
    public async Task RemoveAsync(string key)
    {
        var cacheKey = CacheKeys.Build(_options.KeyPrefix, _options.KeyVersion, key);

        // 删除操作对两种实现都下发，避免 Redis 恢复后读到旧值
        await ExecuteAsync(p => RemoveCoreAsync(p, cacheKey), _redis, "Remove", cacheKey).ConfigureAwait(false);
        await ExecuteAsync(p => RemoveCoreAsync(p, cacheKey), _memory, "Remove", cacheKey).ConfigureAwait(false);
    }

    /// <summary>Redis 不可用时跳过删除，避免无意义的连接异常</summary>
    private static Task RemoveCoreAsync(ICacheProvider provider, string cacheKey)
        => provider.IsAvailable ? provider.RemoveAsync(cacheKey) : Task.CompletedTask;

    /// <summary>
    /// 回填缓存，自动处理空值哨兵、TTL 抖动与降级 TTL 收敛。
    /// </summary>
    private Task SetWithFallbackAsync<T>(string cacheKey, T? value, int durationMinutes) where T : class
    {
        // 空值缓存未开启时写缓存没有意义，保持迁移前"不缓存 null"的语义
        if (value is null && _options.NullValueDurationMinutes <= 0)
        {
            return Task.CompletedTask;
        }

        var provider = SelectProvider();
        var isFallback = ReferenceEquals(provider, _memory);
        var minutes = value is null ? _options.NullValueDurationMinutes : durationMinutes;
        var ttl = ComputeExpiration(minutes, isFallback);

        if (ttl <= TimeSpan.Zero)
        {
            return Task.CompletedTask;
        }

        var entry = CacheEntry<T>.From(value);

        // 记录实际落点，便于确认"是否走了 Redis / 是否降级到内存"
        _logger.LogDebug("[缓存] 回填 {Key} → {Provider}，TTL {Ttl:0.0} 分钟{IsFallback}。",
            cacheKey, provider.Name, ttl.TotalMinutes, isFallback ? "（降级模式）" : string.Empty);

        return ExecuteAsync(p => p.SetAsync(cacheKey, entry, ttl), provider, "Set", cacheKey);
    }

    /// <summary>
    /// 选择本次操作的缓存实现：Redis 可用且未熔断时走 Redis，否则降级到本地内存。
    /// </summary>
    private ICacheProvider SelectProvider()
    {
        if (_options.Provider == CacheProviderKind.Memory)
        {
            return _memory;
        }

        if (!_redis.IsAvailable || IsCircuitOpen())
        {
            return _memory;
        }

        return _redis;
    }

    /// <summary>
    /// 带熔断与降级的执行器：主实现失败时使用本地缓存兜底，任何缓存异常都不影响业务返回。
    /// </summary>
    private async Task<TResult> ExecuteAsync<TResult>(
        Func<ICacheProvider, Task<TResult>> operation,
        TResult fallbackResult,
        ICacheProvider preferred,
        string operationName,
        string cacheKey)
    {
        if (ReferenceEquals(preferred, _redis))
        {
            try
            {
                var result = await operation(_redis).ConfigureAwait(false);
                ResetCircuit();
                return result;
            }
            catch (Exception ex) when (IsTransientCacheException(ex))
            {
                RecordFailure(ex, cacheKey, operationName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[缓存] Redis {Operation} {Key} 发生非预期异常，降级为本地缓存。",
                    operationName, cacheKey);
                RecordFailure(ex, cacheKey, operationName);
            }
        }

        try
        {
            return await operation(_memory).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[缓存] 本地缓存 {Operation} {Key} 失败，本次请求不使用缓存。", operationName, cacheKey);
            return fallbackResult;
        }
    }

    /// <summary>
    /// 无返回值的执行器（写删类操作），语义同泛型版本：主实现失败自动降级到本地内存。
    /// </summary>
    private async Task ExecuteAsync(
        Func<ICacheProvider, Task> operation,
        ICacheProvider preferred,
        string operationName,
        string cacheKey)
    {
        if (ReferenceEquals(preferred, _redis))
        {
            try
            {
                await operation(_redis).ConfigureAwait(false);
                ResetCircuit();
                return;
            }
            catch (Exception ex) when (IsTransientCacheException(ex))
            {
                RecordFailure(ex, cacheKey, operationName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[缓存] Redis {Operation} {Key} 发生非预期异常，降级为本地缓存。",
                    operationName, cacheKey);
                RecordFailure(ex, cacheKey, operationName);
            }
        }

        try
        {
            await operation(_memory).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[缓存] 本地缓存 {Operation} {Key} 失败，已忽略。", operationName, cacheKey);
        }
    }

    private async Task<IDisposable?> TryAcquireDistributedLockAsync(ICacheProvider provider, string cacheKey)
    {
        var lockKey = CacheKeys.BuildLockKey(cacheKey);

        try
        {
            return await provider.AcquireLockAsync(
                lockKey,
                TimeSpan.FromMilliseconds(Math.Max(100, _options.LockExpiryMs)),
                TimeSpan.FromMilliseconds(Math.Max(0, _options.LockAcquireWaitMs))).ConfigureAwait(false);
        }
        catch (Exception ex) when (IsTransientCacheException(ex))
        {
            _logger.LogWarning(ex, "[缓存] 获取分布式锁失败，退化为进程内单飞：{Key}", lockKey);
            return null;
        }
    }

    /// <summary>
    /// 计算带随机抖动的过期时间，打散同一批数据的过期时刻，避免缓存雪崩。<br/>
    /// 公式：TTL = base ± min(base * ratio, MaxJitterMinutes)，下限 60 秒。
    /// </summary>
    private TimeSpan ComputeExpiration(int minutes, bool isFallback)
    {
        if (minutes <= 0)
        {
            minutes = _options.DefaultDurationMinutes;
        }

        // 降级缓存 TTL 收敛，缩短 Redis 恢复后的脏数据窗口
        if (isFallback && _options.FallbackDurationMinutes > 0)
        {
            minutes = Math.Min(minutes, _options.FallbackDurationMinutes);
        }

        var baseSeconds = minutes * 60d;
        var ratio = Math.Clamp(_options.ExpirationJitterRatio, 0d, 0.5d);
        if (ratio <= 0)
        {
            return TimeSpan.FromSeconds(baseSeconds);
        }

        var maxJitterSeconds = Math.Min(baseSeconds * ratio, Math.Max(0, _options.MaxJitterMinutes) * 60d);
        var jitterSeconds = Random.Shared.NextDouble() * 2 * maxJitterSeconds - maxJitterSeconds;

        return TimeSpan.FromSeconds(Math.Max(60, baseSeconds + jitterSeconds));
    }

    private bool IsCircuitOpen()
        => Volatile.Read(ref _circuitOpenUntilUnixMs) > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    private void RecordFailure(Exception exception, string cacheKey, string operationName)
    {
        var failures = Interlocked.Increment(ref _failureCount);
        _logger.LogWarning(exception, "[缓存] Redis {Operation} {Key} 失败（连续失败 {Failures} 次），使用本地缓存降级。",
            operationName, cacheKey, failures);

        if (failures >= Math.Max(1, _options.FailureThreshold))
        {
            var until = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + Math.Max(1000, _options.BreakDurationMs);
            if (Volatile.Read(ref _circuitOpenUntilUnixMs) < until)
            {
                Volatile.Write(ref _circuitOpenUntilUnixMs, until);
                _logger.LogWarning("[缓存] Redis 熔断触发，{Seconds} 秒内缓存访问直接降级到内存。",
                    _options.BreakDurationMs / 1000);
            }
        }
    }

    private void ResetCircuit()
    {
        Interlocked.Exchange(ref _failureCount, 0);
        Volatile.Write(ref _circuitOpenUntilUnixMs, 0);
    }

    /// <summary>判定为"可降级"的缓存异常（网络、超时、连接类），其它异常按错误处理</summary>
    private static bool IsTransientCacheException(Exception exception)
        => exception is RedisException
           or TimeoutException
           or SocketException
           or IOException
           or OperationCanceledException;
}
