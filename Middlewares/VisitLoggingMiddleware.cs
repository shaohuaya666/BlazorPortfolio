using BlazorPortfolio.Models;
using BlazorPortfolio.Services;
using BlazorPortfolio.Services.VisitTracking;
using Microsoft.Extensions.Options;

namespace BlazorPortfolio.Middlewares;

/// <summary>
/// 访问来源采集中间件。<br/>
/// 仅记录页面级请求（GET/HEAD + Accept: text/html），排除静态资源、Blazor 内部通道与 SignalR，
/// 通过非阻塞入队交给后台任务批量落库，采集失败不影响正常请求响应。
/// </summary>
public class VisitLoggingMiddleware
{
    /// <summary>非页面请求前缀：Blazor 电路、SignalR Hub、框架资源、健康检查</summary>
    private static readonly string[] ExcludedPathPrefixes =
    [
        "/_blazor",
        "/portfolioHub",
        "/_framework",
        "/_content",
        "/health"
    ];

    /// <summary>静态资源扩展名，避免缓存失效/爬虫探测产生的资源请求被计入访问</summary>
    private static readonly HashSet<string> ExcludedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".css", ".js", ".mjs", ".map", ".json", ".xml", ".txt",
        ".ico", ".png", ".jpg", ".jpeg", ".gif", ".svg", ".webp", ".avif",
        ".woff", ".woff2", ".ttf", ".otf", ".eot",
        ".pdf", ".zip", ".mp4", ".webm"
    };

    private readonly RequestDelegate _next;
    private readonly MessageQueueService _queue;
    private readonly VisitTrackingOptions _options;
    private readonly ILogger<VisitLoggingMiddleware> _logger;

    public VisitLoggingMiddleware(
        RequestDelegate next,
        MessageQueueService queue,
        IOptions<VisitTrackingOptions> options,
        ILogger<VisitLoggingMiddleware> logger)
    {
        _next = next;
        _queue = queue;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        RecordVisit(context);

        await _next(context);
    }

    /// <summary>
    /// 记录一次页面访问；任何异常都在内部消化，保证请求管道不受影响。
    /// </summary>
    private void RecordVisit(HttpContext context)
    {
        if (!_options.Enabled || !_options.RecordPageViews)
            return;

        try
        {
            if (!ShouldRecord(context))
                return;

            var path = ClientIpResolver.TruncatePath(context.Request.Path.Value ?? "/");

            var recorded = _queue.TryEnqueue(new ChatMessage
            {
                MessageType = "PageView",
                Content = path,
                ClientIp = ClientIpResolver.Resolve(context, _options),
                Timestamp = DateTime.Now
            });

            if (!recorded)
            {
                // 队列已满（通常意味着数据库写入异常），丢弃采集数据以保护请求响应
                _logger.LogDebug("访问记录队列已满，已丢弃本次采集: {Path}", path);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "访问来源采集失败，已忽略");
        }
    }

    /// <summary>
    /// 判断当前请求是否属于需要记录的页面访问。
    /// </summary>
    private static bool ShouldRecord(HttpContext context)
    {
        var request = context.Request;

        if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method))
            return false;

        // WebSocket 升级（SignalR / Blazor 电路）不记录
        if (context.WebSockets.IsWebSocketRequest)
            return false;

        // 只记录期望返回 HTML 页面的请求
        if (!request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase))
            return false;

        var path = request.Path;

        foreach (var prefix in ExcludedPathPrefixes)
        {
            if (path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        var extension = Path.GetExtension(path.Value);
        return string.IsNullOrEmpty(extension) || !ExcludedExtensions.Contains(extension);
    }
}
