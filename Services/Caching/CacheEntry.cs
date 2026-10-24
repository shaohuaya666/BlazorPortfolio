namespace BlazorPortfolio.Services.Caching;

/// <summary>
/// 缓存值包装（信封），用于在分布式缓存中区分"未命中"与"命中但值为 null"。<br/>
/// 这是缓存穿透防护的基础：factory 返回 null 时仍需写入一条短 TTL 哨兵记录。
/// </summary>
/// <typeparam name="T">缓存数据类型</typeparam>
public sealed class CacheEntry<T> where T : class
{
    /// <summary>是否为"空值哨兵"（true 表示业务数据本身就是 null）</summary>
    public bool IsNullSentinel { get; set; }

    /// <summary>业务数据</summary>
    public T? Value { get; set; }

    /// <summary>写入缓存时的 UTC 时间戳（毫秒），仅用于排查问题时判断数据新鲜度</summary>
    public long CreatedAtUnixMs { get; set; }

    /// <summary>由业务数据构造缓存条目</summary>
    public static CacheEntry<T> From(T? value) => new()
    {
        IsNullSentinel = value is null,
        Value = value,
        CreatedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
    };
}
