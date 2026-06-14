using BlazorPortfolio.Models;

namespace BlazorPortfolio.Services;

/// <summary>
/// 作品集数据服务接口，定义所有数据库查询操作
/// </summary>
public interface IPortfolioService
{
    /// <summary>
    /// 异步获取所有个人优势
    /// </summary>
    Task<List<Advantage>> GetAdvantagesAsync();

    /// <summary>
    /// 异步获取所有技术栈分类（含关联标签）
    /// </summary>
    Task<List<SkillCategory>> GetSkillCategoriesAsync();

    /// <summary>
    /// 异步获取所有工作经历（含关联成就）
    /// </summary>
    Task<List<WorkHistory>> GetWorkHistoriesAsync();

    /// <summary>
    /// 异步获取所有项目经历（含关联技能）
    /// </summary>
    Task<List<CompactProject>> GetCompactProjectsAsync();

    /// <summary>
    /// 异步获取技能诊断字典（Key: 标签名）
    /// </summary>
    Task<Dictionary<string, SkillDiagnostic>> GetSkillDiagnosticsAsync();
}
