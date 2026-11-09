using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace BlazorPortfolio.Services.Contact;

/// <summary>
/// 联系表单提交频率限制（Singleton，进程内内存实现）。<br/>
/// 按来源 IP 统计提交时间，限制最小间隔、滑动窗口次数与每日总量，防止表单被用于刷邮件。
/// </summary>
public class ContactRateLimiter
{
    /// <summary>触发一次过期桶清理的桶数量阈值</summary>
    private const int CleanupThreshold = 2000;

    private readonly ConcurrentDictionary<string, Bucket> _buckets = new();
    private readonly IOptions<ContactFormOptions> _options;
    private readonly ILogger<ContactRateLimiter> _logger;

    public ContactRateLimiter(IOptions<ContactFormOptions> options, ILogger<ContactRateLimiter> logger)
    {
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// 尝试为指定来源占用一次提交额度。
    /// </summary>
    /// <param name="clientKey">来源标识（IP）；为空时归入 unknown 桶，避免绕过限制</param>
    /// <returns>允许提交返回 true；超限返回 false</returns>
    public bool TryAcquire(string? clientKey)
    {
        var options = _options.Value;
        if (!options.RateLimitEnabled)
            return true;

        var key = string.IsNullOrWhiteSpace(clientKey) ? "unknown" : clientKey;
        var now = DateTime.Now;
        var bucket = _buckets.GetOrAdd(key, _ => new Bucket());

        lock (bucket)
        {
            // 跨天重置每日计数
            if (bucket.DayStart.Date != now.Date)
            {
                bucket.DayStart = now.Date;
                bucket.DayCount = 0;
            }

            if (bucket.DayCount >= options.PerIpMaxPerDay)
            {
                _logger.LogWarning("联系表单提交被限流：{ClientKey} 已达每日上限 {Max}", key, options.PerIpMaxPerDay);
                return false;
            }

            if (bucket.LastSubmit is { } last
                && (now - last).TotalSeconds < options.MinSubmitIntervalSeconds)
            {
                _logger.LogWarning("联系表单提交被限流：{ClientKey} 提交间隔过短", key);
                return false;
            }

            var windowStart = now.AddMinutes(-Math.Max(1, options.PerIpWindowMinutes));
            bucket.Recent.RemoveAll(t => t < windowStart);
            if (bucket.Recent.Count >= options.PerIpMaxPerWindow)
            {
                _logger.LogWarning("联系表单提交被限流：{ClientKey} 在 {Minutes} 分钟内超过 {Max} 次",
                    key, options.PerIpWindowMinutes, options.PerIpMaxPerWindow);
                return false;
            }

            bucket.Recent.Add(now);
            bucket.LastSubmit = now;
            bucket.DayCount++;
            bucket.LastActivity = now;
        }

        if (_buckets.Count > CleanupThreshold)
            CleanupStaleBuckets(now);

        return true;
    }

    /// <summary>
    /// 清理超过一天未活动的桶，避免大量来源 IP 造成内存持续增长。
    /// </summary>
    private void CleanupStaleBuckets(DateTime now)
    {
        var threshold = now.AddDays(-1);
        foreach (var pair in _buckets)
        {
            if (pair.Value.LastActivity < threshold)
                _buckets.TryRemove(pair.Key, out _);
        }
    }

    private sealed class Bucket
    {
        public List<DateTime> Recent { get; } = [];
        public DateTime? LastSubmit { get; set; }
        public DateTime DayStart { get; set; } = DateTime.Today;
        public int DayCount { get; set; }
        public DateTime LastActivity { get; set; } = DateTime.Now;
    }
}
