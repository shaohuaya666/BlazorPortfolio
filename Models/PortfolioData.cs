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
