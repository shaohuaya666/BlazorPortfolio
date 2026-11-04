using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BlazorPortfolio.Models;

/// <summary>
/// SignalR 消息记录实体，用于持久化存储实时通信数据。
/// 消息先入内存队列，再由后台定时任务批量写入数据库。
/// </summary>
/// <remarks>
/// 复合索引 (MessageType, Timestamp) 供保留期清理任务按时间范围批量删除使用。
/// </remarks>
[Table("ChatMessages")]
[Index(nameof(MessageType), nameof(Timestamp))]
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

    /// <summary>
    /// 客户端真实 IP（明文，兼容 IPv4/IPv6，最长 45 字符）。
    /// 由 ForwardedHeaders 还原代理转发后的来源地址；页面访问与 Hub 连接事件共用该字段。
    /// 无法解析时为 null。
    /// </summary>
    [MaxLength(45)]
    public string? ClientIp { get; set; }

    /// <summary>消息产生时间</summary>
    public DateTime Timestamp { get; set; }
}
