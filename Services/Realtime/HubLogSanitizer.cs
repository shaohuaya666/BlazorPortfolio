using System.Text;

namespace BlazorPortfolio.Services.Realtime;

/// <summary>
/// 系统日志正文的归一化与截断工具。<br/>
/// 目的：阻止客户端通过换行/控制字符伪造日志行，并把正文长度压到数据库列宽以内，
/// 避免 MySQL 严格模式下 "Data too long" 导致整批消息（含联系表单留言）落库失败。
/// </summary>
public static class HubLogSanitizer
{
    /// <summary>
    /// 日志正文最大长度，与 ChatMessages.Content 的 [MaxLength(2000)] 列宽对齐。
    /// </summary>
    public const int MaxContentLength = 2000;

    /// <summary>归一化前的最大扫描长度，避免对超长入参做全量遍历</summary>
    private const int MaxScanLength = MaxContentLength * 2;

    /// <summary>
    /// 归一化日志正文：折叠换行、剔除控制字符、截断至上限。
    /// </summary>
    /// <param name="log">客户端上报的原始日志；可为 null</param>
    /// <returns>可直接入库的正文；无有效内容时返回 null</returns>
    public static string? Normalize(string? log)
    {
        if (string.IsNullOrWhiteSpace(log))
            return null;

        // 每个字符归一化后最多产出 1 个字符，先裁掉多余部分可把单次处理量限制在常量级
        var source = log.Length > MaxScanLength ? log[..MaxScanLength] : log;

        var builder = new StringBuilder(source.Length);
        foreach (var ch in source)
        {
            // 换行折叠为单个空格：一条日志只能占一行，防止伪造多行日志
            if (ch is '\r' or '\n')
            {
                if (builder.Length == 0 || builder[^1] != ' ')
                    builder.Append(' ');

                continue;
            }

            if (ch == '\t' || !char.IsControl(ch))
                builder.Append(ch);
        }

        var normalized = builder.ToString().Trim();
        if (normalized.Length == 0)
            return null;

        if (normalized.Length <= MaxContentLength)
            return normalized;

        var truncated = normalized[..MaxContentLength];

        // 截断点落在代理对中间会切出孤立代理字符，回退一位保证字符串可安全序列化
        if (char.IsHighSurrogate(truncated[^1]))
            truncated = truncated[..^1];

        return truncated;
    }
}
