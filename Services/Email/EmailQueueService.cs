using System.Threading.Channels;

namespace BlazorPortfolio.Services.Email;

/// <summary>
/// 邮件通知内存队列（Singleton）。<br/>
/// 与消息持久化队列彼此独立：SMTP 发送慢或失败不会反压消息落库链路。<br/>
/// 投递方为 MessagePersistService（落库成功后转发），消费方为 EmailNotificationService。
/// </summary>
public class EmailQueueService
{
    private const int DefaultCapacity = 1000;

    private readonly Channel<EmailNotification> _channel;
    private readonly HashSet<int> _pendingMessageIds = new();
    private readonly object _sync = new();

    public EmailQueueService(int capacity = DefaultCapacity)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _channel = Channel.CreateBounded<EmailNotification>(options);
    }

    /// <summary>
    /// 尝试以非阻塞方式入队。
    /// 队列已满、消息已关闭或同一消息已在队列中时返回 false，由调用方决定是否记录日志。
    /// </summary>
    public bool TryEnqueue(EmailNotification notification)
    {
        lock (_sync)
        {
            if (!_pendingMessageIds.Add(notification.MessageId))
                return false;
        }

        if (_channel.Writer.TryWrite(notification))
            return true;

        lock (_sync)
        {
            _pendingMessageIds.Remove(notification.MessageId);
        }

        return false;
    }

    /// <summary>
    /// 异步等待队列中出现可读数据（消费者端）。
    /// 配合 <see cref="TryDequeue"/> 使用，可安全地实现"等待超时后回到主循环"的逻辑，
    /// 避免直接对 ReadAsync 取消造成消息丢失。
    /// </summary>
    public async ValueTask<bool> WaitToReadAsync(CancellationToken ct = default)
    {
        return await _channel.Reader.WaitToReadAsync(ct);
    }

    /// <summary>
    /// 尝试取出一条待发邮件（消费者端，非阻塞）。
    /// </summary>
    public bool TryDequeue(out EmailNotification? notification)
    {
        if (_channel.Reader.TryRead(out var item))
        {
            RemovePending(item.MessageId);
            notification = item;
            return true;
        }

        notification = null;
        return false;
    }

    /// <summary>
    /// 当前队列中待发送的邮件数量（近似值）。
    /// </summary>
    public int PendingCount => _channel.Reader.Count;

    private void RemovePending(int messageId)
    {
        lock (_sync)
        {
            _pendingMessageIds.Remove(messageId);
        }
    }
}
