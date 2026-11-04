using BlazorPortfolio.Data;
using BlazorPortfolio.Models;
using BlazorPortfolio.Services.VisitTracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BlazorPortfolio.Services;

/// <summary>
/// 访问记录保留期清理后台服务。<br/>
/// 按配置周期删除超过保留天数的非业务消息（PageView/Connection/Disconnection/SystemLog），
/// 采用"先取主键、再分批删除"的方式，避免单条大 DELETE 长时间锁表；
/// Contact 联系表单消息属于业务数据，不在清理范围内。
/// </summary>
public class VisitLogCleanupService : BackgroundService
{
    /// <summary>参与保留期清理的消息类型</summary>
    private static readonly string[] PurgeableMessageTypes =
    [
        "PageView",
        "Connection",
        "Disconnection",
        "SystemLog"
    ];

    /// <summary>服务启动后的首轮延迟，避开应用启动与数据库连接初始化高峰</summary>
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(1);

    private readonly IDbContextFactory<PortfolioDbContext> _dbFactory;
    private readonly VisitTrackingOptions _options;
    private readonly ILogger<VisitLogCleanupService> _logger;

    public VisitLogCleanupService(
        IDbContextFactory<PortfolioDbContext> dbFactory,
        IOptions<VisitTrackingOptions> options,
        ILogger<VisitLogCleanupService> logger)
    {
        _dbFactory = dbFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // RetentionDays <= 0 表示永久保留，不启动清理循环
        if (_options.RetentionDays <= 0)
        {
            _logger.LogInformation("访问记录保留期为 {RetentionDays} 天，已禁用自动清理", _options.RetentionDays);
            return;
        }

        var interval = TimeSpan.FromHours(Math.Max(1, _options.CleanupIntervalHours));
        _logger.LogInformation(
            "访问记录清理服务已启动：保留 {RetentionDays} 天，执行间隔 {Interval} 小时",
            _options.RetentionDays, interval.TotalHours);

        try
        {
            await Task.Delay(InitialDelay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // 清理失败不影响主流程，下一轮重试
                _logger.LogWarning(ex, "访问记录清理失败，将在下一个周期重试");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("访问记录清理服务已停止");
    }

    /// <summary>
    /// 执行一轮清理：按批次删除截止时间之前的可清理消息，单轮最多执行配置的批次数。
    /// </summary>
    private async Task CleanupOnceAsync(CancellationToken ct)
    {
        var cutoff = DateTime.Now.AddDays(-_options.RetentionDays);
        var batchSize = Math.Clamp(_options.CleanupBatchSize, 100, 10000);
        var maxBatches = Math.Max(1, _options.MaxCleanupBatchesPerRound);

        var totalDeleted = 0;

        for (var batch = 0; batch < maxBatches; batch++)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            // 先取主键，控制单次删除范围，避免长事务与锁表
            var ids = await db.Set<ChatMessage>()
                .AsNoTracking()
                .Where(m => PurgeableMessageTypes.Contains(m.MessageType) && m.Timestamp < cutoff)
                .OrderBy(m => m.Id)
                .Select(m => m.Id)
                .Take(batchSize)
                .ToListAsync(ct);

            if (ids.Count == 0)
                break;

            var deleted = await db.Set<ChatMessage>()
                .Where(m => ids.Contains(m.Id))
                .ExecuteDeleteAsync(ct);

            totalDeleted += deleted;

            // 本批未取满，说明已无更多过期数据
            if (ids.Count < batchSize)
                break;
        }

        if (totalDeleted > 0)
        {
            _logger.LogInformation(
                "访问记录清理完成：删除 {Count} 条早于 {Cutoff:yyyy-MM-dd HH:mm:ss} 的记录", totalDeleted, cutoff);
        }
        else
        {
            _logger.LogDebug("访问记录清理完成：无过期数据（截止 {Cutoff:yyyy-MM-dd HH:mm:ss}）", cutoff);
        }
    }
}
