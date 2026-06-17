namespace BlazorPortfolio.Attributes;

/// <summary>
/// 标记接口方法需要自动启用本地缓存。<br/>
/// 标注此特性的方法在调用时会自动执行缓存优先策略：
/// 先查缓存，未命中则执行实际逻辑并将结果回填缓存。
/// </summary>
/// <remarks>
/// 使用示例：
/// <code>
/// [Cached(Key = "Portfolio_Advantages", DurationMinutes = 30)]
/// Task&lt;List&lt;Advantage&gt;&gt; GetAdvantagesAsync();
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class CachedAttribute : Attribute
{
    /// <summary>
    /// 缓存 Key，全局唯一标识
    /// </summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>
    /// 缓存过期时间（分钟），默认 30 分钟
    /// </summary>
    public int DurationMinutes { get; init; } = 30;
}
