using BlazorPortfolio.Services.Realtime;
using Xunit;

namespace BlazorPortfolio.Tests.Services;

/// <summary>
/// 系统日志正文归一化与截断规则的单元测试。
/// </summary>
public class HubLogSanitizerTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\r\n\t")]
    public void EmptyOrWhitespace_ShouldReturnNull(string? log)
    {
        Assert.Null(HubLogSanitizer.Normalize(log));
    }

    [Fact]
    public void LineBreaks_ShouldBeCollapsedToSpaces()
    {
        var result = HubLogSanitizer.Normalize("第一行\r\n伪造的断连日志");

        Assert.Equal("第一行 伪造的断连日志", result);
        Assert.NotNull(result);
        Assert.False(result!.Contains('\n'));
        Assert.False(result.Contains('\r'));
    }

    [Fact]
    public void ControlCharacters_ShouldBeRemovedButTabKept()
    {
        var result = HubLogSanitizer.Normalize("技能\t切换\u0007\u001b[31m");

        Assert.Equal("技能\t切换[31m", result);
    }

    [Fact]
    public void OversizedContent_ShouldBeTruncatedToLimit()
    {
        var result = HubLogSanitizer.Normalize(new string('a', 100_000));

        Assert.NotNull(result);
        Assert.Equal(HubLogSanitizer.MaxContentLength, result!.Length);
    }

    [Fact]
    public void Truncation_ShouldNotSplitSurrogatePair()
    {
        // 1999 个 ASCII + 1 个 emoji（占 2 个 char），截断点正好落在代理对中间
        var log = new string('a', HubLogSanitizer.MaxContentLength - 1) + "😀";

        var result = HubLogSanitizer.Normalize(log);

        Assert.NotNull(result);
        Assert.True(result!.Length <= HubLogSanitizer.MaxContentLength);
        Assert.False(char.IsHighSurrogate(result[^1]));
    }

    [Fact]
    public void NormalizedContent_ShouldNeverExceedColumnWidth()
    {
        var log = string.Concat(Enumerable.Repeat("标签切换\r\n", 5000));

        var result = HubLogSanitizer.Normalize(log);

        Assert.NotNull(result);
        Assert.True(result!.Length <= HubLogSanitizer.MaxContentLength);
    }
}
