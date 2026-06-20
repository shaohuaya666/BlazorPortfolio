using System.Threading.Channels;
using BlazorPortfolio.Models;

namespace BlazorPortfolio.Services;

/// <summary>
/// 消息内存队列服务（Singleton）。
/// 基于 System.Threading.Channels 实现高性能生产者-消费者模式，
/// SignalR Hub 作为生产者写入消息，MessagePersistService 作为消费者批量写出到数据库。
/// </summary>
public class MessageQueueService
{
    private readonly Channel<ChatMessage> _channel;

    public MessageQueueService(int capacity = 10000)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait  // 队列满时阻塞等待，避免丢消息
        };
        _channel = Channel.CreateBounded<ChatMessage>(options);
    }

    /// <summary>
    /// 将一条消息加入队列（生产者端）。
    /// </summary>
    public async ValueTask EnqueueAsync(ChatMessage message, CancellationToken ct = default)
    {
        await _channel.Writer.WriteAsync(message, ct);
    }

    /// <summary>
    /// 从队列中批量读取消息（消费者端）。
    /// 当队列为空时立即返回空集合，不会阻塞等待。
    /// </summary>
    /// <param name="maxBatchSize">单次最大读取条数</param>
    public List<ChatMessage> DequeueBatch(int maxBatchSize = 100)
    {
        var batch = new List<ChatMessage>(maxBatchSize);

        while (batch.Count < maxBatchSize && _channel.Reader.TryRead(out var message))
        {
            batch.Add(message);
        }

        return batch;
    }

    /// <summary>
    /// 异步等待并读取一条消息（用于持续消费场景）。
    /// </summary>
    public async ValueTask<ChatMessage> ReadAsync(CancellationToken ct = default)
    {
        return await _channel.Reader.ReadAsync(ct);
    }

    /// <summary>
    /// 当前队列中待处理的消息数量（近似值）。
    /// </summary>
    public int PendingCount => _channel.Reader.Count;
}
