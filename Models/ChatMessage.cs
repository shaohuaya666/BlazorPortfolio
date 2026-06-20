using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlazorPortfolio.Models;

/// <summary>
/// SignalR 消息记录实体，用于持久化存储实时通信数据。
/// 消息先入内存队列，再由后台定时任务批量写入数据库。
/// </summary>
[Table("ChatMessages")]
public class ChatMessage : IEntity
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>消息类型：Contact / SystemLog / Connection / Disconnection</summary>
    [MaxLength(50)]
    public string MessageType { get; set; } = string.Empty;

    /// <summary>发送者名称（联系表单用）</summary>
    [MaxLength(200)]
    public string? SenderName { get; set; }

    /// <summary>发送者邮箱（联系表单用）</summary>
    [MaxLength(200)]
    public string? SenderEmail { get; set; }

    /// <summary>消息正文</summary>
    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;

    /// <summary>SignalR 连接 ID</summary>
    [MaxLength(100)]
    public string? ConnectionId { get; set; }

    /// <summary>消息产生时间</summary>
    public DateTime Timestamp { get; set; }
}
