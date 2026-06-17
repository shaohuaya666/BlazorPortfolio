using BlazorPortfolio.Attributes;
using BlazorPortfolio.Models;

namespace BlazorPortfolio.Services;

/// <summary>
/// 作品集数据服务接口，定义所有数据库查询操作。<br/>
/// 标注了 <see cref="CachedAttribute"/> 的方法会默认启用缓存优先策略。
/// </summary>
public interface IPortfolioService
{
    /// <summary>
    /// 异步获取所有个人优势
    /// </summary>
    [Cached(Key = "Portfolio_Advantages", DurationMinutes = 30)]
    Task<List<Advantage>> GetAdvantagesAsync();

    /// <summary>
    /// 异步获取所有技术栈分类（含关联标签）
    /// </summary>
    [Cached(Key = "Portfolio_SkillCategories", DurationMinutes = 30)]
    Task<List<SkillCategory>> GetSkillCategoriesAsync();

    /// <summary>
    /// 异步获取所有工作经历（含关联成就）
    /// </summary>
    [Cached(Key = "Portfolio_WorkHistories", DurationMinutes = 30)]
    Task<List<WorkHistory>> GetWorkHistoriesAsync();

    /// <summary>
    /// 异步获取所有项目经历（含关联技能）
    /// </summary>
    [Cached(Key = "Portfolio_CompactProjects", DurationMinutes = 30)]
    Task<List<CompactProject>> GetCompactProjectsAsync();

    /// <summary>
    /// 异步获取技能诊断字典（Key: 标签名）
    /// </summary>
    [Cached(Key = "Portfolio_SkillDiagnostics", DurationMinutes = 30)]
    Task<Dictionary<string, SkillDiagnostic>> GetSkillDiagnosticsAsync();
}
