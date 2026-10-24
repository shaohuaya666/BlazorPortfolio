using System.Text.RegularExpressions;

namespace BlazorPortfolio.Services.Caching;

/// <summary>
/// 缓存键命名与校验规则。<br/>
/// 统一格式：<c>{前缀}:{版本}:{业务键}</c>，例如 <c>BlazorPortfolio:v1:Portfolio_Advantages</c>。<br/>
/// 分布式锁键在缓存键基础上追加 <c>:lock</c> 后缀。
/// </summary>
public static class CacheKeys
{
    /// <summary>缓存键最大长度限制（Redis 官方建议不超过 1KB，这里收紧便于运维排查）</summary>
    private const int MaxKeyLength = 256;

    /// <summary>允许出现在键中的字符：字母、数字、下划线、中划线、英文冒号、英文点</summary>
    private static readonly Regex InvalidCharacters = new(@"[^A-Za-z0-9_\-.:]", RegexOptions.Compiled);

    /// <summary>
    /// 生成规范化后的完整缓存键。
    /// </summary>
    /// <param name="prefix">全局前缀，取自 <see cref="CacheOptions.KeyPrefix"/></param>
    /// <param name="version">键版本，取自 <see cref="CacheOptions.KeyVersion"/></param>
    /// <param name="businessKey">业务键，建议仅使用字母数字与下划线</param>
    /// <returns>完整缓存键</returns>
    public static string Build(string prefix, string version, string businessKey)
    {
        if (string.IsNullOrWhiteSpace(businessKey))
        {
            throw new ArgumentException("缓存业务键不能为空。", nameof(businessKey));
        }

        var business = InvalidCharacters.Replace(businessKey.Trim(), "_");
        var versionPart = string.IsNullOrWhiteSpace(version) ? "v1" : version.Trim();
        var prefixPart = string.IsNullOrWhiteSpace(prefix) ? "App" : prefix.Trim();

        var key = $"{prefixPart}:{versionPart}:{business}";

        if (key.Length > MaxKeyLength)
        {
            throw new ArgumentException($"缓存键长度超过上限 {MaxKeyLength}：{key}", nameof(businessKey));
        }

        return key;
    }

    /// <summary>由缓存键派生分布式锁键</summary>
    public static string BuildLockKey(string cacheKey) => $"{cacheKey}:lock";
}
