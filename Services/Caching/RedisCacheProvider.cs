using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace BlazorPortfolio.Services.Caching;

/// <summary>
/// 基于 StackExchange.Redis 的分布式缓存实现（主实现）。<br/>
/// 特性：<br/>
/// 1. 懒连接 + 后台重连，Redis 不可用不会阻塞应用启动；<br/>
/// 2. JSON 序列化（System.Text.Json），与 Store/读取 端对称，兼容双向导航属性；<br/>
/// 3. 写入前做体积校验，拒绝大 Key；<br/>
/// 4. 基于 SET NX + Lua 校验释放的单实例分布式锁（缓存击穿防护）。<br/>
/// </summary>
/// <remarks>
/// 未选用 <c>Microsoft.Extensions.Caching.StackExchangeRedis</c>（IDistributedCache）的原因：
/// 该封装不提供分布式锁能力，且序列化策略不可控，无法满足击穿防护的要求。
/// </remarks>
public sealed class RedisCacheProvider : ICacheProvider, IDisposable
{
    /// <summary>
    /// JSON 序列化配置：<br/>
    /// - camelCase 命名：压缩体积、与其它语言客户端对齐；<br/>
    /// - 忽略 null：减小 payload；<br/>
    /// - IgnoreCycles：EF Core 导航存在双向引用（SkillCategory↔TagInfo），不忽略会抛异常；<br/>
    /// - UnsafeRelaxedJsonEscaping：保留中文不被转义为 \uXXXX。
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    /// <summary>释放分布式锁的 Lua 脚本：先校验持有者令牌，避免误删其它实例的锁</summary>
    private const string ReleaseLockScript =
        "if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('del', KEYS[1]) else return 0 end";

    private readonly RedisCacheOptions _options;
    private readonly ILogger<RedisCacheProvider> _logger;
    private readonly long _maxValueSizeBytes;
    private readonly Lazy<ConnectionMultiplexer?> _connection;
    private bool _disposed;

    public RedisCacheProvider(IOptions<CacheOptions> options, ILogger<RedisCacheProvider> logger)
    {
        _options = options.Value.Redis;
        _logger = logger;
        _maxValueSizeBytes = options.Value.MaxValueSizeKb <= 0
            ? long.MaxValue
            : options.Value.MaxValueSizeKb * 1024L;

        // 连接延迟到首次使用；复杂类型/字符串为空时视为未配置，直接让调用方降级
        _connection = new Lazy<ConnectionMultiplexer?>(CreateConnection, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <inheritdoc />
    public string Name => "Redis";

    /// <inheritdoc />
    public bool IsAvailable => _connection.Value?.IsConnected == true;

    /// <inheritdoc />
    public bool SupportsDistributedLock => true;

    private ConnectionMultiplexer? CreateConnection()
    {
        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            _logger.LogWarning("[缓存] 未配置 Cache:Redis:ConnectionString，Redis 缓存不可用，将降级为本地内存缓存。");
            return null;
        }

        try
        {
            var configuration = ConfigurationOptions.Parse(_options.ConnectionString);
            configuration.AbortOnConnectFail = _options.AbortOnConnectFail;
            configuration.ConnectTimeout = _options.ConnectTimeoutMs;
            configuration.SyncTimeout = _options.SyncTimeoutMs;
            configuration.ConnectRetry = _options.ConnectRetry;
            configuration.Ssl = _options.Ssl;
            configuration.ClientName = _options.ClientName;
            configuration.DefaultDatabase = _options.Database;
            // 断线后由 StackExchange.Redis 默认重连策略（指数退避）恢复，上层熔断负责降级兜底

            var connection = ConnectionMultiplexer.Connect(configuration);

            connection.ConnectionFailed += (_, e) =>
                _logger.LogWarning("[缓存] Redis 连接失败：{EndPoint} - {FailureType}，触发本地缓存降级。",
                    e.EndPoint, e.FailureType);
            connection.ConnectionRestored += (_, _) =>
                _logger.LogInformation("[缓存] Redis 连接已恢复，缓存访问将自动切回 Redis。");

            _logger.LogInformation("[缓存] Redis 已配置：{EndPoints}，数据库 {Database}。",
                string.Join(",", configuration.EndPoints), _options.Database);

            return connection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[缓存] Redis 初始化失败，后续请求将降级为本地内存缓存。");
            return null;
        }
    }

    /// <summary>
    /// 获取数据库句柄。连接不可用时抛出 <see cref="RedisConnectionException"/>，交由上层熔断降级处理。
    /// </summary>
    private IDatabase GetDatabase()
    {
        var connection = _connection.Value;
        if (connection is null || !connection.IsConnected)
        {
            throw new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Redis 连接不可用。");
        }

        return connection.GetDatabase(_options.Database);
    }

    /// <inheritdoc />
    public async Task<CacheEntry<T>?> GetAsync<T>(string cacheKey, CancellationToken cancellationToken = default)
        where T : class
    {
        var raw = await GetDatabase().StringGetAsync(cacheKey).ConfigureAwait(false);
        if (raw.IsNullOrEmpty)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<CacheEntry<T>>((string?)raw ?? string.Empty, JsonOptions);
        }
        catch (JsonException ex)
        {
            // 反序列化失败（多为结构升级导致），删除脏数据并当作未命中处理
            _logger.LogWarning(ex, "[缓存] 键 {Key} 反序列化失败，已自动删除脏数据并回源。", cacheKey);
            await GetDatabase().KeyDeleteAsync(cacheKey).ConfigureAwait(false);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string cacheKey, CacheEntry<T> entry, TimeSpan ttl,
        CancellationToken cancellationToken = default) where T : class
    {
        if (ttl <= TimeSpan.Zero)
        {
            // 兜底：禁止写入永不过期的键
            ttl = TimeSpan.FromMinutes(1);
        }

        var json = JsonSerializer.Serialize(entry, JsonOptions);
        var size = Encoding.UTF8.GetByteCount(json);

        if (size > _maxValueSizeBytes)
        {
            _logger.LogWarning("[缓存] 键 {Key} 序列化后约 {Size}KB，超过上限 {Limit}KB，已跳过写入。",
                cacheKey, size / 1024, _maxValueSizeBytes / 1024);
            return;
        }

        await GetDatabase().StringSetAsync(cacheKey, json, ttl, When.Always).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task RemoveAsync(string cacheKey, CancellationToken cancellationToken = default)
        => GetDatabase().KeyDeleteAsync(cacheKey);

    /// <inheritdoc />
    public async Task<IDisposable?> AcquireLockAsync(string lockKey, TimeSpan expiry, TimeSpan wait,
        CancellationToken cancellationToken = default)
    {
        var db = GetDatabase();
        var token = Guid.NewGuid().ToString("N");
        var deadline = DateTime.UtcNow + wait;

        do
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await db.StringSetAsync(lockKey, token, expiry, When.NotExists).ConfigureAwait(false))
            {
                return new RedisDistributedLock(db, lockKey, token);
            }

            await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken).ConfigureAwait(false);
        } while (DateTime.UtcNow < deadline);

        return null;
    }

    /// <summary>分布式锁句柄，释放时通过 Lua 脚本校验令牌</summary>
    private sealed class RedisDistributedLock : IDisposable
    {
        private readonly IDatabase _database;
        private readonly string _key;
        private readonly string _token;
        private bool _released;

        public RedisDistributedLock(IDatabase database, string key, string token)
        {
            _database = database;
            _key = key;
            _token = token;
        }

        public void Dispose()
        {
            if (_released)
            {
                return;
            }

            _released = true;

            try
            {
                _database.ScriptEvaluateAsync(ReleaseLockScript,
                        keys: new RedisKey[] { _key },
                        values: new RedisValue[] { _token })
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (Exception)
            {
                // 锁过期或连接异常：无需处理，锁会在 expiry 到期后自动释放
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_connection.IsValueCreated)
        {
            _connection.Value?.Dispose();
        }
    }
}
