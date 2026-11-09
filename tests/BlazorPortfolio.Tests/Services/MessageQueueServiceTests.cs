using BlazorPortfolio.Models;
using BlazorPortfolio.Services;
using Xunit;

namespace BlazorPortfolio.Tests.Services;

/// <summary>
/// 消息内存队列的生产/消费行为测试（不依赖数据库与网络）。
/// </summary>
public class MessageQueueServiceTests
{
    private static ChatMessage NewMessage(string content = "/") => new()
    {
        MessageType = "PageView",
        Content = content,
        ClientIp = "127.0.0.1",
        Timestamp = DateTime.Now
    };

    [Fact]
    public async Task EnqueueThenDequeue_ShouldKeepMessageAndClearQueue()
    {
        var queue = new MessageQueueService();

        await queue.EnqueueAsync(NewMessage("/contact"));

        var batch = queue.DequeueBatch();

        var message = Assert.Single(batch);
        Assert.Equal("/contact", message.Content);
        Assert.Equal("PageView", message.MessageType);
        Assert.Equal(0, queue.PendingCount);
    }

    [Fact]
    public async Task DequeueBatch_ShouldRespectMaxBatchSize()
    {
        var queue = new MessageQueueService();
        for (var i = 0; i < 5; i++)
        {
            await queue.EnqueueAsync(NewMessage($"/page-{i}"));
        }

        var batch = queue.DequeueBatch(maxBatchSize: 2);

        Assert.Equal(2, batch.Count);
        Assert.Equal(3, queue.PendingCount);
    }

    [Fact]
    public void DequeueBatch_OnEmptyQueue_ShouldReturnEmptyList()
    {
        var queue = new MessageQueueService();

        Assert.Empty(queue.DequeueBatch());
    }

    [Fact]
    public void TryEnqueue_WhenCapacityExhausted_ShouldReturnFalse()
    {
        var queue = new MessageQueueService(capacity: 1);

        Assert.True(queue.TryEnqueue(NewMessage()));
        Assert.False(queue.TryEnqueue(NewMessage()));
        Assert.Equal(1, queue.PendingCount);
    }
}
