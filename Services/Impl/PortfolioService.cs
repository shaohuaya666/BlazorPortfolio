using BlazorPortfolio.Data;
using BlazorPortfolio.Models;
using Microsoft.EntityFrameworkCore;

namespace BlazorPortfolio.Services.Impl;

/// <summary>
/// 作品集数据服务实现，通过 EF Core DbContextFactory 查询 MySQL 数据库。<br/>
/// 缓存逻辑由 <see cref="CacheService"/> 统一封装，此处仅负责数据库查询。
/// 各方法的缓存 Key 和过期时间由接口 <see cref="IPortfolioService"/> 上的 <see cref="Attributes.CachedAttribute"/> 特性定义。
/// </summary>
public class PortfolioService : IPortfolioService
{
    private readonly IDbContextFactory<PortfolioDbContext> _dbFactory;
    private readonly CacheService _cacheService;

    public PortfolioService(IDbContextFactory<PortfolioDbContext> dbFactory, CacheService cacheService)
    {
        _dbFactory = dbFactory;
        _cacheService = cacheService;
    }

    /// <summary>
    /// 异步获取所有个人优势列表（带缓存）
    /// </summary>
    public Task<List<Advantage>> GetAdvantagesAsync()
    {
        return _cacheService.GetOrSetAsync<List<Advantage>>("Portfolio_Advantages", async () =>
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.Set<Advantage>().AsNoTracking().ToListAsync();
        });
    }

    /// <summary>
    /// 异步获取所有技术栈分类，同时预加载关联的技术标签（带缓存）
    /// </summary>
    public Task<List<SkillCategory>> GetSkillCategoriesAsync()
    {
        return _cacheService.GetOrSetAsync<List<SkillCategory>>("Portfolio_SkillCategories", async () =>
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.Set<SkillCategory>()
                .AsNoTracking()
                .Include(c => c.Tags)
                .ToListAsync();
        });
    }

    /// <summary>
    /// 异步获取所有工作经历，同时预加载关联的成就列表（带缓存）
    /// </summary>
    public Task<List<WorkHistory>> GetWorkHistoriesAsync()
    {
        return _cacheService.GetOrSetAsync<List<WorkHistory>>("Portfolio_WorkHistories", async () =>
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.Set<WorkHistory>()
                .AsNoTracking()
                .Include(w => w.Achievements)
                .ToListAsync();
        });
    }

    /// <summary>
    /// 异步获取所有项目经历，同时预加载关联的技能列表（带缓存）
    /// </summary>
    public Task<List<CompactProject>> GetCompactProjectsAsync()
    {
        return _cacheService.GetOrSetAsync<List<CompactProject>>("Portfolio_CompactProjects", async () =>
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.Set<CompactProject>()
                .AsNoTracking()
                .Include(p => p.Skills)
                .ToListAsync();
        });
    }

    /// <summary>
    /// 异步获取技能诊断字典，以标签名称作为 Key 方便前端按名称查找（带缓存）
    /// </summary>
    public Task<Dictionary<string, SkillDiagnostic>> GetSkillDiagnosticsAsync()
    {
        return _cacheService.GetOrSetAsync<Dictionary<string, SkillDiagnostic>>("Portfolio_SkillDiagnostics", async () =>
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var list = await db.Set<SkillDiagnostic>().AsNoTracking().ToListAsync();
            return list.ToDictionary(d => d.TagName);
        });
    }
}
