using BlazorPortfolio.Data;
using BlazorPortfolio.Hubs;
using BlazorPortfolio.Middlewares;
using BlazorPortfolio.Services;
using BlazorPortfolio.Services.Impl;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ASP.NET Core 默认按以下优先级加载配置（后者覆盖前者）：
// 1. appsettings.json                          — 基础配置（提交到 Git）
// 2. appsettings.{Environment}.json             — 环境特定覆盖（含敏感信息，不提交）
// 3. 环境变量 / 命令行参数                       — 运行时覆盖
// 切换环境：在 launchSettings.json 中修改 ASPNETCORE_ENVIRONMENT
//   可选值：Development / Staging / Production

// 注册内存缓存服务（用于本地缓存数据库查询结果）
builder.Services.AddMemoryCache();

// 注册 Blazor Server 交互式组件
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// 注册 SignalR 实时通信服务
builder.Services.AddSignalR();

// 配置 Entity Framework Core + MySQL 数据库
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContextFactory<PortfolioDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// 注册通用缓存服务（Singleton，与 IMemoryCache 生命周期一致）
builder.Services.AddSingleton<CacheService>();

// 通过接口注入注册 PortfolioService
builder.Services.AddScoped<IPortfolioService, PortfolioService>();

// 配置 Antiforgery 防护选项
builder.Services.AddAntiforgery(options =>
{
    options.SuppressXFrameOptionsHeader = false;
});

var app = builder.Build();

// 配置 HTTP 请求管道

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
app.UseAntiforgery();

// 映射 Blazor Server 交互式渲染模式
app.MapRazorComponents<BlazorPortfolio.App>()
    .AddInteractiveServerRenderMode();

// 映射 SignalR Hub 端点
app.MapHub<PortfolioHub>("/portfolioHub");

app.Run();
