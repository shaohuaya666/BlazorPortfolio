using BlazorPortfolio.Data;
using BlazorPortfolio.Models;
using Microsoft.EntityFrameworkCore;

namespace BlazorPortfolio.Services.Impl;

/// <summary>
/// 作品集数据服务实现，通过 EF Core DbContextFactory 查询 MySQL 数据库
/// </summary>
public class PortfolioService : IPortfolioService
{
    /// <summary>
    /// DbContext 工厂，用于创建短生命周期 DbContext 实例（适配 Blazor Server 长连接场景）
    /// </summary>
    private readonly IDbContextFactory<PortfolioDbContext> _dbFactory;

    /// <summary>
    /// 构造函数，注入 DbContext 工厂
    /// </summary>
    /// <param name="dbFactory">EF Core DbContext 工厂实例</param>
    public PortfolioService(IDbContextFactory<PortfolioDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    /// <summary>
    /// 异步获取所有个人优势列表
    /// </summary>
    public async Task<List<Advantage>> GetAdvantagesAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Advantages.AsNoTracking().ToListAsync();
    }

    /// <summary>
    /// 异步获取所有技术栈分类，同时预加载关联的技术标签
    /// </summary>
    public async Task<List<SkillCategory>> GetSkillCategoriesAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.SkillCategories
            .AsNoTracking()
            .Include(c => c.Tags)
            .ToListAsync();
    }

    /// <summary>
    /// 异步获取所有工作经历，同时预加载关联的成就列表
    /// </summary>
    public async Task<List<WorkHistory>> GetWorkHistoriesAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.WorkHistories
            .AsNoTracking()
            .Include(w => w.Achievements)
            .ToListAsync();
    }

    /// <summary>
    /// 异步获取所有项目经历，同时预加载关联的技能列表
    /// </summary>
    public async Task<List<CompactProject>> GetCompactProjectsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.CompactProjects
            .AsNoTracking()
            .Include(p => p.Skills)
            .ToListAsync();
    }

    /// <summary>
    /// 异步获取技能诊断字典，以标签名称作为 Key 方便前端按名称查找
    /// </summary>
    public async Task<Dictionary<string, SkillDiagnostic>> GetSkillDiagnosticsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var list = await db.SkillDiagnostics.AsNoTracking().ToListAsync();
        return list.ToDictionary(d => d.TagName);
    }
}
