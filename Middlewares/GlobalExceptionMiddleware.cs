using System.Net;
using System.Text.Json;

namespace BlazorPortfolio.Middlewares;

/// <summary>
/// 全局异常捕获中间件
/// 拦截管道中所有未处理的异常，记录日志并返回统一错误响应
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "未处理的异常: {Message}", ex.Message);

            // SignalR / Blazor WebSocket 连接异常让框架自行处理
            if (IsSignalRRequest(context) || IsBlazorWebSocket(context))
            {
                return;
            }

            // API 请求（JSON 预期）返回统一错误 JSON
            if (IsApiRequest(context))
            {
                await HandleExceptionAsync(context, ex);
                return;
            }

            // Blazor 页面请求：重新抛出异常，交给 UseExceptionHandler("/Error") 跳转错误页面
            throw;
        }
    }

    /// <summary>
    /// 判断是否为 API 请求（期望返回 JSON 而非 HTML 页面）
    /// </summary>
    private static bool IsApiRequest(HttpContext context)
    {
        return context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
               || context.Request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase);
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/json; charset=utf-8";

        var response = new ErrorResponse
        {
            TraceId = context.TraceIdentifier,
            Message = "服务器内部错误，请稍后重试。",
            Detail = _env.IsDevelopment() ? exception.ToString() : null
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _env.IsDevelopment()
        });

        await context.Response.WriteAsync(json);
    }

    private static bool IsSignalRRequest(HttpContext context)
    {
        return context.Request.Path.StartsWithSegments("/portfolioHub", StringComparison.OrdinalIgnoreCase)
               || context.WebSockets.IsWebSocketRequest;
    }

    private static bool IsBlazorWebSocket(HttpContext context)
    {
        // Blazor Server 使用 WebSocket 连接 _blazor 路径
        return context.Request.Path.StartsWithSegments("/_blazor", StringComparison.OrdinalIgnoreCase);
    }
}

public record ErrorResponse
{
    public string TraceId { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? Detail { get; init; }
}
