using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlazorPortfolio.Models;

/// <summary>
/// 个人优势实体
/// </summary>
public class Advantage
{
    /// <summary>
    /// 主键
    /// </summary>
    [Key]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 序号
    /// </summary>
    public string Num { get; set; } = string.Empty;

    /// <summary>
    /// 优势标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 优势描述
    /// </summary>
    public string Desc { get; set; } = string.Empty;
}

/// <summary>
/// 技术栈分类实体
/// </summary>
public class SkillCategory
{
    /// <summary>
    /// 主键
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 分类名称
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 主题色标识
    /// </summary>
    public string ThemeColor { get; set; } = "primary";

    /// <summary>
    /// 关联的技术标签列表
    /// </summary>
    public List<TagInfo> Tags { get; set; } = new();
}

/// <summary>
/// 技术标签实体
/// </summary>
public class TagInfo
{
    /// <summary>
    /// 主键
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 标签名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 是否为核心技能
    /// </summary>
    public bool IsCore { get; set; }

    /// <summary>
    /// 所属分类ID（外键）
    /// </summary>
    public int SkillCategoryId { get; set; }

    /// <summary>
    /// 所属分类导航属性
    /// </summary>
    [ForeignKey(nameof(SkillCategoryId))]
    public SkillCategory? SkillCategory { get; set; }
}

/// <summary>
/// 工作经历实体
/// </summary>
public class WorkHistory
{
    /// <summary>
    /// 主键
    /// </summary>
    [Key]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 公司名称
    /// </summary>
    public string Company { get; set; } = string.Empty;

    /// <summary>
    /// 职位
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// 任职期间
    /// </summary>
    public string Period { get; set; } = string.Empty;

    /// <summary>
    /// 工作描述
    /// </summary>
    public string Desc { get; set; } = string.Empty;

    /// <summary>
    /// 是否为当前在职
    /// </summary>
    public bool IsCurrent { get; set; }

    /// <summary>
    /// 关联的成就列表
    /// </summary>
    public List<Achievement> Achievements { get; set; } = new();
}

/// <summary>
/// 工作成就实体
/// </summary>
public class Achievement
{
    /// <summary>
    /// 主键
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 成就描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 所属工作经历ID（外键）
    /// </summary>
    public string WorkHistoryId { get; set; } = string.Empty;

    /// <summary>
    /// 所属工作经历导航属性
    /// </summary>
    [ForeignKey(nameof(WorkHistoryId))]
    public WorkHistory? WorkHistory { get; set; }
}

/// <summary>
/// 项目经历实体
/// </summary>
public class CompactProject
{
    /// <summary>
    /// 主键
    /// </summary>
    [Key]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 项目名称
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 项目类型
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 项目年份
    /// </summary>
    public string Year { get; set; } = string.Empty;

    /// <summary>
    /// 项目描述
    /// </summary>
    public string Desc { get; set; } = string.Empty;

    /// <summary>
    /// 关联的技能列表
    /// </summary>
    public List<ProjectSkill> Skills { get; set; } = new();
}

/// <summary>
/// 项目技能关联实体
/// </summary>
public class ProjectSkill
{
    /// <summary>
    /// 主键
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 技能名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 所属项目ID（外键）
    /// </summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>
    /// 所属项目导航属性
    /// </summary>
    [ForeignKey(nameof(ProjectId))]
    public CompactProject? Project { get; set; }
}

/// <summary>
/// 技能诊断详情实体
/// </summary>
public class SkillDiagnostic
{
    /// <summary>
    /// 主键
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 技术标签名称
    /// </summary>
    public string TagName { get; set; } = string.Empty;

    /// <summary>
    /// 诊断描述
    /// </summary>
    public string Desc { get; set; } = string.Empty;

    /// <summary>
    /// 统计/等级信息
    /// </summary>
    public string Stat { get; set; } = string.Empty;

    /// <summary>
    /// 运行状态
    /// </summary>
    public string Status { get; set; } = string.Empty;
}
