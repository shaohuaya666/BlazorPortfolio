using BlazorPortfolio.Services.Contact;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace BlazorPortfolio.Tests.Services;

/// <summary>
/// 联系表单限流规则的单元测试（进程内实现，可确定性执行）。
/// </summary>
public class ContactRateLimiterTests
{
    private const string ClientIp = "203.0.113.10";

    private static ContactRateLimiter CreateLimiter(ContactFormOptions options) =>
        new(Options.Create(options), NullLogger<ContactRateLimiter>.Instance);

    [Fact]
    public void RateLimitDisabled_ShouldAlwaysAllow()
    {
        var limiter = CreateLimiter(new ContactFormOptions { RateLimitEnabled = false });

        for (var i = 0; i < 10; i++)
        {
            Assert.True(limiter.TryAcquire(ClientIp));
        }
    }

    [Fact]
    public void SecondSubmitWithinMinInterval_ShouldBeRejected()
    {
        var limiter = CreateLimiter(new ContactFormOptions
        {
            MinSubmitIntervalSeconds = 30,
            PerIpMaxPerWindow = 10,
            PerIpMaxPerDay = 10
        });

        Assert.True(limiter.TryAcquire(ClientIp));
        Assert.False(limiter.TryAcquire(ClientIp));
    }

    [Fact]
    public void SubmitExceedingWindowQuota_ShouldBeRejected()
    {
        var limiter = CreateLimiter(new ContactFormOptions
        {
            MinSubmitIntervalSeconds = 0,
            PerIpWindowMinutes = 10,
            PerIpMaxPerWindow = 2,
            PerIpMaxPerDay = 10
        });

        Assert.True(limiter.TryAcquire(ClientIp));
        Assert.True(limiter.TryAcquire(ClientIp));
        Assert.False(limiter.TryAcquire(ClientIp));
    }

    [Fact]
    public void SubmitExceedingDailyQuota_ShouldBeRejected()
    {
        var limiter = CreateLimiter(new ContactFormOptions
        {
            MinSubmitIntervalSeconds = 0,
            PerIpWindowMinutes = 1,
            PerIpMaxPerWindow = 100,
            PerIpMaxPerDay = 2
        });

        Assert.True(limiter.TryAcquire(ClientIp));
        Assert.True(limiter.TryAcquire(ClientIp));
        Assert.False(limiter.TryAcquire(ClientIp));
    }

    [Fact]
    public void DifferentClients_ShouldHaveIndependentQuota()
    {
        var limiter = CreateLimiter(new ContactFormOptions
        {
            MinSubmitIntervalSeconds = 30,
            PerIpMaxPerWindow = 10,
            PerIpMaxPerDay = 10
        });

        Assert.True(limiter.TryAcquire(ClientIp));
        Assert.False(limiter.TryAcquire(ClientIp));
        Assert.True(limiter.TryAcquire("198.51.100.7"));
    }

    [Fact]
    public void EmptyClientKey_ShouldFallIntoUnknownBucket()
    {
        var limiter = CreateLimiter(new ContactFormOptions
        {
            MinSubmitIntervalSeconds = 30,
            PerIpMaxPerWindow = 10,
            PerIpMaxPerDay = 10
        });

        Assert.True(limiter.TryAcquire(null));
        Assert.False(limiter.TryAcquire(string.Empty));
    }
}
