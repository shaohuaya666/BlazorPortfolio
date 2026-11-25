using BlazorPortfolio.Services.Realtime;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace BlazorPortfolio.Tests.Services;

/// <summary>
/// 系统日志限流规则的单元测试（进程内实现，可确定性执行）。
/// </summary>
public class HubLogRateLimiterTests
{
    private const string ClientKey = "203.0.113.10|connection-1";

    private static HubLogRateLimiter CreateLimiter(HubLogOptions options) =>
        new(Options.Create(options), NullLogger<HubLogRateLimiter>.Instance);

    [Fact]
    public void RateLimitDisabled_ShouldAlwaysAllow()
    {
        var limiter = CreateLimiter(new HubLogOptions
        {
            RateLimitEnabled = false,
            MaxPerBurstWindow = 1,
            MaxPerWindow = 1
        });

        for (var i = 0; i < 10; i++)
        {
            Assert.Equal(HubLogRateLimitDecision.Allowed, limiter.TryAcquire(ClientKey));
        }
    }

    [Fact]
    public void ExceedingBurstQuota_ShouldBeThrottled()
    {
        var limiter = CreateLimiter(new HubLogOptions
        {
            BurstWindowSeconds = 60,
            MaxPerBurstWindow = 3,
            WindowSeconds = 60,
            MaxPerWindow = 100,
            AbortAfterConsecutiveRejections = 0
        });

        Assert.Equal(HubLogRateLimitDecision.Allowed, limiter.TryAcquire(ClientKey));
        Assert.Equal(HubLogRateLimitDecision.Allowed, limiter.TryAcquire(ClientKey));
        Assert.Equal(HubLogRateLimitDecision.Allowed, limiter.TryAcquire(ClientKey));
        Assert.Equal(HubLogRateLimitDecision.Throttled, limiter.TryAcquire(ClientKey));
    }

    [Fact]
    public void ExceedingWindowQuota_ShouldBeThrottled()
    {
        var limiter = CreateLimiter(new HubLogOptions
        {
            BurstWindowSeconds = 1,
            MaxPerBurstWindow = 100,
            WindowSeconds = 60,
            MaxPerWindow = 2,
            AbortAfterConsecutiveRejections = 0
        });

        Assert.Equal(HubLogRateLimitDecision.Allowed, limiter.TryAcquire(ClientKey));
        Assert.Equal(HubLogRateLimitDecision.Allowed, limiter.TryAcquire(ClientKey));
        Assert.Equal(HubLogRateLimitDecision.Throttled, limiter.TryAcquire(ClientKey));
    }

    [Fact]
    public void ConsecutiveRejections_ShouldEscalateToAbort()
    {
        var limiter = CreateLimiter(new HubLogOptions
        {
            BurstWindowSeconds = 60,
            MaxPerBurstWindow = 1,
            WindowSeconds = 60,
            MaxPerWindow = 1,
            AbortAfterConsecutiveRejections = 3
        });

        Assert.Equal(HubLogRateLimitDecision.Allowed, limiter.TryAcquire(ClientKey));
        Assert.Equal(HubLogRateLimitDecision.Throttled, limiter.TryAcquire(ClientKey));
        Assert.Equal(HubLogRateLimitDecision.Throttled, limiter.TryAcquire(ClientKey));
        Assert.Equal(HubLogRateLimitDecision.Abort, limiter.TryAcquire(ClientKey));
    }

    [Fact]
    public void DifferentClients_ShouldHaveIndependentQuota()
    {
        var limiter = CreateLimiter(new HubLogOptions
        {
            BurstWindowSeconds = 60,
            MaxPerBurstWindow = 1,
            WindowSeconds = 60,
            MaxPerWindow = 1
        });

        Assert.Equal(HubLogRateLimitDecision.Allowed, limiter.TryAcquire(ClientKey));
        Assert.Equal(HubLogRateLimitDecision.Throttled, limiter.TryAcquire(ClientKey));
        Assert.Equal(HubLogRateLimitDecision.Allowed, limiter.TryAcquire("198.51.100.7|connection-2"));
    }

    [Fact]
    public void DifferentScopes_ShouldHaveIndependentQuota()
    {
        var limiter = CreateLimiter(new HubLogOptions
        {
            BurstWindowSeconds = 60,
            MaxPerBurstWindow = 1,
            WindowSeconds = 60,
            MaxPerWindow = 1
        });

        Assert.Equal(HubLogRateLimitDecision.Allowed,
            limiter.TryAcquire(ClientKey, HubLogRateLimitScope.Log));
        Assert.Equal(HubLogRateLimitDecision.Throttled,
            limiter.TryAcquire(ClientKey, HubLogRateLimitScope.Log));

        // 日志额度耗尽不应影响订阅动作
        Assert.Equal(HubLogRateLimitDecision.Allowed,
            limiter.TryAcquire(ClientKey, HubLogRateLimitScope.Subscription));
    }

    [Fact]
    public void EmptyClientKey_ShouldFallIntoUnknownBucket()
    {
        var limiter = CreateLimiter(new HubLogOptions
        {
            BurstWindowSeconds = 60,
            MaxPerBurstWindow = 1,
            WindowSeconds = 60,
            MaxPerWindow = 1
        });

        Assert.Equal(HubLogRateLimitDecision.Allowed, limiter.TryAcquire(null));
        Assert.Equal(HubLogRateLimitDecision.Throttled, limiter.TryAcquire(string.Empty));
    }
}
