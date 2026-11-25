using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace BlazorPortfolio.Services.Realtime;

/// <summary>日志限流判定结果</summary>
public enum HubLogRateLimitDecision
{
    /// <summary>放行</summary>
    Allowed,

    /// <summary>超限，静默丢弃本次日志</summary>
    Throttled,

    /// <summary>连续超限，建议断开连接</summary>
    Abort
}

/// <summary>限流作用域：不同作用域使用独立配额，互不挤占</summary>
public enum HubLogRateLimitScope
{
    /// <summary>日志推送</summary>
    Log,

    /// <summary>订阅 / 退订实时日志</summary>
    Subscription
}

/// <summary>
/// 系统日志推送的频率限制（Singleton，进程内内存实现）。<br/>
/// 与 <c>ContactRateLimiter</c> 完全独立：日志调用不消耗联系表单的提交额度。
/// 采用「突发窗口 + 滑动窗口」双阈值，超限静默丢弃，连续超限可要求断开连接，
/// 避免单个客户端用垃圾日志填满 MessageQueueService（其满队列会阻塞所有 Hub 方法）。
/// </summary>
public sealed class HubLogRateLimiter
{
    /// <summary>触发一次过期桶清理的桶数量阈值</summary>
    private const int CleanupThreshold = 2000;

    /// <summary>桶的闲置回收时长：远大于限流窗口，回收后计数重置不影响正常客户端</summary>
    private static readonly TimeSpan IdleRetention = TimeSpan.FromMinutes(10);

    private readonly ConcurrentDictionary<string, Bucket> _buckets = new();
    private readonly IOptions<HubLogOptions> _options;
    private readonly ILogger<HubLogRateLimiter> _logger;

    public HubLogRateLimiter(IOptions<HubLogOptions> options, ILogger<HubLogRateLimiter> logger)
    {
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// 尝试为指定来源占用一次额度。
    /// </summary>
    /// <param name="clientKey">来源标识（建议 IP + ConnectionId 组合）；为空时归入 unknown 桶，避免绕过限制</param>
    /// <param name="scope">作用域，决定使用哪一份独立配额</param>
    public HubLogRateLimitDecision TryAcquire(string? clientKey, HubLogRateLimitScope scope = HubLogRateLimitScope.Log)
    {
        var options = _options.Value;
        if (!options.RateLimitEnabled)
            return HubLogRateLimitDecision.Allowed;

        var source = string.IsNullOrWhiteSpace(clientKey) ? "unknown" : clientKey;
        var key = $"{scope}:{source}";
        var now = DateTime.UtcNow;
        var bucket = _buckets.GetOrAdd(key, _ => new Bucket());

        // 阈值配置为 0 或负数时按 1 处理，避免误配成「全部拒绝」
        var maxPerBurst = Math.Max(1, options.MaxPerBurstWindow);
        var maxPerWindow = Math.Max(1, options.MaxPerWindow);
        var burstWindow = TimeSpan.FromSeconds(Math.Max(1, options.BurstWindowSeconds));
        var window = TimeSpan.FromSeconds(Math.Max(options.BurstWindowSeconds, options.WindowSeconds));

        HubLogRateLimitDecision decision;
        var consecutiveRejections = 0;
        lock (bucket)
        {
            bucket.LastActivity = now;
            bucket.Burst.RemoveAll(t => t < now - burstWindow);
            bucket.Window.RemoveAll(t => t < now - window);

            if (bucket.Burst.Count >= maxPerBurst || bucket.Window.Count >= maxPerWindow)
            {
                bucket.ConsecutiveRejections++;
                decision = options.AbortAfterConsecutiveRejections > 0
                           && bucket.ConsecutiveRejections >= options.AbortAfterConsecutiveRejections
                    ? HubLogRateLimitDecision.Abort
                    : HubLogRateLimitDecision.Throttled;
            }
            else
            {
                bucket.Burst.Add(now);
                bucket.Window.Add(now);
                bucket.ConsecutiveRejections = 0;
                decision = HubLogRateLimitDecision.Allowed;
            }

            consecutiveRejections = bucket.ConsecutiveRejections;
        }

        if (decision != HubLogRateLimitDecision.Allowed)
        {
            _logger.LogDebug("Hub 调用被限流：{Key} -> {Decision}（连续超限 {Count} 次）",
                key, decision, consecutiveRejections);
        }

        if (_buckets.Count > CleanupThreshold)
            CleanupStaleBuckets(now);

        return decision;
    }

    /// <summary>
    /// 清理长时间无活动的桶，避免大量来源标识造成内存持续增长。
    /// </summary>
    private void CleanupStaleBuckets(DateTime now)
    {
        var threshold = now - IdleRetention;
        foreach (var pair in _buckets)
        {
            if (pair.Value.LastActivity < threshold)
                _buckets.TryRemove(pair.Key, out _);
        }
    }

    private sealed class Bucket
    {
        /// <summary>突发窗口内的放行时间戳</summary>
        public List<DateTime> Burst { get; } = [];

        /// <summary>滑动窗口内的放行时间戳</summary>
        public List<DateTime> Window { get; } = [];

        /// <summary>连续被限流次数，放行一次即清零</summary>
        public int ConsecutiveRejections { get; set; }

        public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    }
}
