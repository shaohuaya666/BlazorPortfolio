using System.Net;

namespace BlazorPortfolio.Services.VisitTracking;

/// <summary>
/// 客户端 IP 解析与归一化工具。<br/>
/// 优先使用 ForwardedHeaders 中间件还原后的 <see cref="HttpContext.Connection.RemoteIpAddress"/>，
/// 避免手工解析 X-Forwarded-For 多值列表导致多级代理场景误判。
/// </summary>
public static class ClientIpResolver
{
    /// <summary>写入数据库的路径最大长度，超出截断，避免超长 URL 撑大 Content 字段</summary>
    public const int MaxPathLength = 500;

    /// <summary>
    /// 解析当前请求的客户端 IP，返回归一化后的字符串；无法解析时返回 null。
    /// </summary>
    /// <param name="context">当前 HTTP 上下文</param>
    /// <param name="options">采集配置，用于读取可选的真实 IP 头</param>
    public static string? Resolve(HttpContext context, VisitTrackingOptions options)
    {
        // 1. 显式配置的真实 IP 头（仅 CDN 等受控边缘节点场景启用）
        if (!string.IsNullOrWhiteSpace(options.RealIpHeader)
            && context.Request.Headers.TryGetValue(options.RealIpHeader, out var headerValue))
        {
            var candidate = headerValue.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();

            if (IPAddress.TryParse(candidate, out var headerAddress))
            {
                return Format(headerAddress);
            }
        }

        // 2. 框架已按 ForwardedHeaders 配置还原的远端地址
        return Format(context.Connection.RemoteIpAddress);
    }

    /// <summary>
    /// 归一化 IP 表示：IPv4-mapped IPv6（::ffff:1.2.3.4）统一转为 IPv4，
    /// 避免同一客户端在库中出现两种写法；IPv6 去掉作用域后缀。
    /// </summary>
    public static string? Format(IPAddress? address)
    {
        if (address is null)
            return null;

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }
        else if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 && address.ScopeId != 0)
        {
            // 链路本地地址带 %scope 后缀，去掉后便于统计与查询
            address = new IPAddress(address.GetAddressBytes());
        }

        return address.ToString();
    }

    /// <summary>
    /// 截断访问路径，只保留路径部分（不记录查询串），降低敏感参数落库风险。
    /// </summary>
    public static string TruncatePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return "/";

        return path.Length <= MaxPathLength ? path : path[..MaxPathLength];
    }
}
