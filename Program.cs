using BlazorPortfolio.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// 添加 SignalR 服务
builder.Services.AddSignalR();

// 配置 Antiforgery 选项
builder.Services.AddAntiforgery(options =>
{
    options.SuppressXFrameOptionsHeader = false;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<BlazorPortfolio.App>()
    .AddInteractiveServerRenderMode();

// 映射 SignalR Hub
app.MapHub<PortfolioHub>("/portfolioHub");

app.Run();
