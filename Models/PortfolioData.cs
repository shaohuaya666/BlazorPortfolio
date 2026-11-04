using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlazorPortfolio.Models;

/// <summary>
/// 实体标记接口，用于反射自动发现并注册到 DbContext
/// </summary>
public interface IEntity { }

/// <summary>
/// 个人优势实体
/// </summary>
[Table("Advantages")]
public class Advantage : IEntity
{
    [Key, MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [MaxLength(10)]
    public string Num { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Desc { get; set; } = string.Empty;
}

/// <summary>
/// 技术栈分类实体
/// </summary>
[Table("SkillCategories")]
public class SkillCategory : IEntity
{
    [Key]
    public int Id { get; set; }

    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(50)]
    public string ThemeColor { get; set; } = "primary";

    public List<TagInfo> Tags { get; set; } = new();
}

/// <summary>
/// 技术标签实体
/// </summary>
[Table("Tags")]
public class TagInfo : IEntity
{
    [Key]
    public int Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public bool IsCore { get; set; }

    public int SkillCategoryId { get; set; }

    [ForeignKey(nameof(SkillCategoryId))]
    public SkillCategory? SkillCategory { get; set; }
}

/// <summary>
/// 工作经历实体
/// </summary>
[Table("WorkHistories")]
public class WorkHistory : IEntity
{
    [Key, MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Company { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Role { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Period { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Desc { get; set; } = string.Empty;

    public bool IsCurrent { get; set; }

    public List<Achievement> Achievements { get; set; } = new();
}

/// <summary>
/// 工作成就实体
/// </summary>
[Table("Achievements")]
public class Achievement : IEntity
{
    [Key]
    public int Id { get; set; }

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(50)]
    public string WorkHistoryId { get; set; } = string.Empty;

    [ForeignKey(nameof(WorkHistoryId))]
    public WorkHistory? WorkHistory { get; set; }
}

/// <summary>
/// 项目经历实体
/// </summary>
[Table("CompactProjects")]
public class CompactProject : IEntity
{
    [Key, MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Type { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Year { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Desc { get; set; } = string.Empty;

    public List<ProjectSkill> Skills { get; set; } = new();
}

/// <summary>
/// 项目技能关联实体
/// </summary>
[Table("ProjectSkills")]
public class ProjectSkill : IEntity
{
    [Key]
    public int Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string ProjectId { get; set; } = string.Empty;

    [ForeignKey(nameof(ProjectId))]
    public CompactProject? Project { get; set; }
}

/// <summary>
/// 重点项目实体：项目经历区块的核心展示卡片，内容完全由数据库驱动
/// </summary>
[Table("FeaturedProjects")]
public class FeaturedProject : IEntity
{
    [Key, MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    /// <summary>卡片徽标文本，如 "核心设计 · 全栈"</summary>
    [MaxLength(100)]
    public string Badge { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>技术栈标签串，使用 "|" 分隔，如 ".NET 8|ASP.NET Core"</summary>
    [MaxLength(500)]
    public string TechStack { get; set; } = string.Empty;

    /// <summary>项目背景与职责概述</summary>
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>挑战与解决方案，为空时隐藏对应区块</summary>
    [MaxLength(1000)]
    public string Challenge { get; set; } = string.Empty;

    /// <summary>成果与影响，为空时隐藏对应区块</summary>
    [MaxLength(1000)]
    public string Result { get; set; } = string.Empty;

    /// <summary>交互演示类型：latency=延迟滑块 / devices=设备计数 / both=两者，NULL=无</summary>
    [MaxLength(20)]
    public string? Demo { get; set; }

    /// <summary>排序权重，小者在前</summary>
    public int SortOrder { get; set; }

    public List<FeaturedProjectTab> Tabs { get; set; } = new();

    public List<FeaturedProjectMetric> Metrics { get; set; } = new();
}

/// <summary>
/// 重点项目标签页实体：卡片内的可切换职责明细面板
/// </summary>
[Table("FeaturedProjectTabs")]
public class FeaturedProjectTab : IEntity
{
    [Key]
    public int Id { get; set; }

    /// <summary>标签页标识，如 flow / review，用于选中态与 SignalR 日志</summary>
    [MaxLength(50)]
    public string TabKey { get; set; } = string.Empty;

    /// <summary>标签按钮显示文本，如 "流程管理"</summary>
    [MaxLength(50)]
    public string Label { get; set; } = string.Empty;

    /// <summary>面板标题，如 "流程引擎与动态规则体系"</summary>
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>职责明细，多条以换行分隔</summary>
    [MaxLength(2000)]
    public string Lines { get; set; } = string.Empty;

    /// <summary>排序权重，小者在前</summary>
    public int SortOrder { get; set; }

    [MaxLength(50)]
    public string ProjectId { get; set; } = string.Empty;

    [ForeignKey(nameof(ProjectId))]
    public FeaturedProject? Project { get; set; }
}

/// <summary>
/// 重点项目量化指标实体：卡片侧栏的成果指标卡片
/// </summary>
[Table("FeaturedProjectMetrics")]
public class FeaturedProjectMetric : IEntity
{
    [Key]
    public int Id { get; set; }

    /// <summary>指标值，如 "数万笔/日"</summary>
    [MaxLength(100)]
    public string Value { get; set; } = string.Empty;

    /// <summary>指标说明，如 "交易请求处理量"</summary>
    [MaxLength(200)]
    public string Label { get; set; } = string.Empty;

    /// <summary>排序权重，小者在前</summary>
    public int SortOrder { get; set; }

    [MaxLength(50)]
    public string ProjectId { get; set; } = string.Empty;

    [ForeignKey(nameof(ProjectId))]
    public FeaturedProject? Project { get; set; }
}

/// <summary>
/// 技能诊断详情实体
/// </summary>
[Table("SkillDiagnostics")]
public class SkillDiagnostic : IEntity
{
    [Key]
    public int Id { get; set; }

    [MaxLength(100)]
    public string TagName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Desc { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Stat { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Status { get; set; } = string.Empty;
}
