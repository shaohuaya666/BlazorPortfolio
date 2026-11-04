using System.Net;
using BlazorPortfolio.Data;
using BlazorPortfolio.Hubs;
using BlazorPortfolio.Middlewares;
using BlazorPortfolio.Services;
using BlazorPortfolio.Services.Impl;
using BlazorPortfolio.Services.Caching;
using BlazorPortfolio.Services.VisitTracking;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// ASP.NET Core 默认按以下优先级加载配置（后者覆盖前者）：
// 1. appsettings.json                          — 基础配置（提交到 Git）
// 2. appsettings.{Environment}.json             — 环境特定覆盖（含敏感信息，不提交）
// 3. 环境变量 / 命令行参数                       — 运行时覆盖
// 切换环境：在 launchSettings.json 中修改 ASPNETCORE_ENVIRONMENT
//   可选值：Development / Staging / Production

// 注册内存缓存服务：
// 1. Redis 正常时作为降级后备（见 CacheService.SelectProvider）；
// 2. Redis 未配置 / 熔断时作为主实现，保证应用始终可用。
builder.Services.AddMemoryCache();

// 绑定缓存配置（appsettings.json 的 "Cache" 节点，可由环境变量 Cache__Redis__ConnectionString 覆盖）
builder.Services.Configure<CacheOptions>(builder.Configuration.GetSection(CacheOptions.SectionName));

// 注册 Blazor Server 交互式组件
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// 注册 SignalR 实时通信服务
builder.Services.AddSignalR();

// 配置 Entity Framework Core + MySQL 数据库
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContextFactory<PortfolioDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// 注册缓存访问层的两种实现（Redis 为主实现，Memory 为降级实现）
// Redis 采用懒连接：Redis 不可用不影响应用启动，运行时会自动降级
builder.Services.AddSingleton<RedisCacheProvider>();
builder.Services.AddSingleton<MemoryCacheProvider>();

// 注册通用缓存服务（Singleton，与 IMemoryCache 生命周期一致）
// 上层业务仅依赖 CacheService，是否走 Redis 由配置与运行时健康状态决定
builder.Services.AddSingleton(sp => new CacheService(
    sp.GetRequiredService<RedisCacheProvider>(),
    sp.GetRequiredService<MemoryCacheProvider>(),
    sp.GetRequiredService<IOptions<CacheOptions>>(),
    sp.GetRequiredService<ILogger<CacheService>>()));

// 注册消息队列服务（Singleton，SignalR Hub 和后台服务共享同一实例）
builder.Services.AddSingleton<MessageQueueService>();

// 注册消息持久化后台服务（Hosted Service，定时从队列取数据写入数据库）
builder.Services.AddHostedService<MessagePersistService>();

// ---- 访问来源 IP 采集 ----
// 绑定采集配置（appsettings.json 的 "VisitTracking" 节点，可由环境变量 VisitTracking__xxx 覆盖）
var visitTrackingOptions = builder.Configuration
    .GetSection(VisitTrackingOptions.SectionName)
    .Get<VisitTrackingOptions>() ?? new VisitTrackingOptions();
builder.Services.Configure<VisitTrackingOptions>(builder.Configuration.GetSection(VisitTrackingOptions.SectionName));

// 配置转发头：只采纳来自可信代理的 X-Forwarded-For / X-Forwarded-Proto，
// 由框架把结果写回 Connection.RemoteIpAddress，业务代码无需手工解析转发头。
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = Math.Max(1, visitTrackingOptions.ForwardedHeadersLimit);

    // 清空默认的 KnownProxies/KnownNetworks，完全由配置决定信任范围，避免默认开放过宽
    options.KnownProxies.Clear();
    options.KnownNetworks.Clear();

    foreach (var proxy in visitTrackingOptions.TrustedProxies ?? [])
    {
        if (IPEndPoint.TryParse(proxy, out var endpoint))
        {
            options.KnownProxies.Add(endpoint.Address);
        }
    }
});

// 注册访问记录保留期清理后台服务
builder.Services.AddHostedService<VisitLogCleanupService>();

// 通过接口注入注册 PortfolioService
builder.Services.AddScoped<IPortfolioService, PortfolioService>();

// 配置 Antiforgery 防护选项
builder.Services.AddAntiforgery(options =>
{
    options.SuppressXFrameOptionsHeader = false;
});

var app = builder.Build();

// 配置 HTTP 请求管道

// 转发头处理必须位于管道最前，否则后续取到的是反向代理地址而非真实客户端 IP
app.UseForwardedHeaders();

// 全局异常捕获中间件（放置在最前面以拦截所有下游异常）
app.UseMiddleware<GlobalExceptionMiddleware>();

// 始终使用自定义错误页面（开发/生产统一），而不是默认开发者异常页
app.UseExceptionHandler("/Error", createScopeForErrors: true);

if (!app.Environment.IsDevelopment())
{
    // HSTS 仅在生产环境启用
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// 访问来源采集：位于静态文件之后，避免静态资源与探测请求污染访问记录
app.UseMiddleware<VisitLoggingMiddleware>();

app.UseAntiforgery();

// 映射 Blazor Server 交互式渲染模式
app.MapRazorComponents<BlazorPortfolio.App>()
    .AddInteractiveServerRenderMode();

// 映射 SignalR Hub 端点
app.MapHub<PortfolioHub>("/portfolioHub");

app.Run();
