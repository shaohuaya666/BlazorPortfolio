using BlazorPortfolio.Models;
using Microsoft.EntityFrameworkCore;

namespace BlazorPortfolio.Data;

/// <summary>
/// 作品集数据库上下文，管理所有实体映射与种子数据
/// </summary>
public class PortfolioDbContext : DbContext
{
    /// <summary>
    /// 构造函数，接收 EF Core 配置选项
    /// </summary>
    public PortfolioDbContext(DbContextOptions<PortfolioDbContext> options) : base(options)
    {
    }

    /// <summary>个人优势</summary>
    public DbSet<Advantage> Advantages => Set<Advantage>();

    /// <summary>技术栈分类</summary>
    public DbSet<SkillCategory> SkillCategories => Set<SkillCategory>();

    /// <summary>技术标签</summary>
    public DbSet<TagInfo> Tags => Set<TagInfo>();

    /// <summary>工作经历</summary>
    public DbSet<WorkHistory> WorkHistories => Set<WorkHistory>();

    /// <summary>工作成就</summary>
    public DbSet<Achievement> Achievements => Set<Achievement>();

    /// <summary>项目经历</summary>
    public DbSet<CompactProject> CompactProjects => Set<CompactProject>();

    /// <summary>项目技能关联</summary>
    public DbSet<ProjectSkill> ProjectSkills => Set<ProjectSkill>();

    /// <summary>技能诊断详情</summary>
    public DbSet<SkillDiagnostic> SkillDiagnostics => Set<SkillDiagnostic>();

    /// <summary>
    /// 配置实体映射：表名、字段约束、外键关系及级联删除策略
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Advantage 配置
        modelBuilder.Entity<Advantage>(entity =>
        {
            entity.ToTable("Advantages");
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.Num).HasMaxLength(10);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Desc).HasMaxLength(500);
        });

        // SkillCategory -> TagInfo (1:N)
        modelBuilder.Entity<SkillCategory>(entity =>
        {
            entity.ToTable("SkillCategories");
            entity.Property(e => e.Title).HasMaxLength(100);
            entity.Property(e => e.ThemeColor).HasMaxLength(50);
            entity.HasMany(e => e.Tags)
                .WithOne(t => t.SkillCategory)
                .HasForeignKey(t => t.SkillCategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // TagInfo 配置
        modelBuilder.Entity<TagInfo>(entity =>
        {
            entity.ToTable("Tags");
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        // WorkHistory -> Achievement (1:N)
        modelBuilder.Entity<WorkHistory>(entity =>
        {
            entity.ToTable("WorkHistories");
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.Company).HasMaxLength(200);
            entity.Property(e => e.Role).HasMaxLength(200);
            entity.Property(e => e.Period).HasMaxLength(50);
            entity.Property(e => e.Desc).HasMaxLength(500);
            entity.HasMany(e => e.Achievements)
                .WithOne(a => a.WorkHistory)
                .HasForeignKey(a => a.WorkHistoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Achievement 配置
        modelBuilder.Entity<Achievement>(entity =>
        {
            entity.ToTable("Achievements");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.WorkHistoryId).HasMaxLength(50);
        });

        // CompactProject -> ProjectSkill (1:N)
        modelBuilder.Entity<CompactProject>(entity =>
        {
            entity.ToTable("CompactProjects");
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Type).HasMaxLength(100);
            entity.Property(e => e.Year).HasMaxLength(50);
            entity.Property(e => e.Desc).HasMaxLength(500);
            entity.HasMany(e => e.Skills)
                .WithOne(s => s.Project)
                .HasForeignKey(s => s.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ProjectSkill 配置
        modelBuilder.Entity<ProjectSkill>(entity =>
        {
            entity.ToTable("ProjectSkills");
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.ProjectId).HasMaxLength(50);
        });

        // SkillDiagnostic 配置
        modelBuilder.Entity<SkillDiagnostic>(entity =>
        {
            entity.ToTable("SkillDiagnostics");
            entity.Property(e => e.TagName).HasMaxLength(100);
            entity.Property(e => e.Desc).HasMaxLength(500);
            entity.Property(e => e.Stat).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(50);
        });

        // 填充种子数据
        SeedData(modelBuilder);
    }

    /// <summary>
    /// 初始化种子数据，用于开发环境预填充数据库
    /// </summary>
    private static void SeedData(ModelBuilder modelBuilder)
    {
        // 个人优势
        modelBuilder.Entity<Advantage>().HasData(
            new Advantage { Id = "adv-1", Num = "01", Title = "15年企业级开发经验", Desc = "长期从事制造业信息化系统研发，具备完整项目开发与交付经验。" },
            new Advantage { Id = "adv-2", Num = "02", Title = "业务系统设计能力", Desc = "参与PLM、PVS、RDMS等系统建设，具备业务建模与模块设计能力。" },
            new Advantage { Id = "adv-3", Num = "03", Title = "设备集成开发", Desc = "具备RS232、RS485、RFID、TCP/IP等设备接入开发经验。" },
            new Advantage { Id = "adv-4", Num = "04", Title = "SQL性能优化", Desc = "擅长复杂SQL分析、执行计划优化及大型视图性能调优。" },
            new Advantage { Id = "adv-5", Num = "05", Title = "全栈开发能力", Desc = "掌握.NET后端开发、前端页面开发及桌面端应用开发。" },
            new Advantage { Id = "adv-6", Num = "06", Title = "独立交付能力", Desc = "能够独立完成需求分析、开发测试及系统上线工作。" },
            new Advantage { Id = "adv-7", Num = "07", Title = "持续学习能力", Desc = "持续关注AI辅助开发、研发效能提升及.NET技术生态。" },
            new Advantage { Id = "adv-8", Num = "08", Title = "团队协作能力", Desc = "具备跨部门沟通经验，能够高效推进项目落地。" }
        );

        // 技术栈分类
        modelBuilder.Entity<SkillCategory>().HasData(
            new SkillCategory { Id = 1, Title = "后端开发", ThemeColor = "tertiary" },
            new SkillCategory { Id = 2, Title = "前端 & 桌面端", ThemeColor = "secondary" },
            new SkillCategory { Id = 3, Title = "设备通信", ThemeColor = "primary" },
            new SkillCategory { Id = 4, Title = "数据库", ThemeColor = "muted" },
            new SkillCategory { Id = 5, Title = "中间件 & 运维", ThemeColor = "muted" }
        );

        // 技术标签
        modelBuilder.Entity<TagInfo>().HasData(
            new TagInfo { Id = 1, Name = ".NET 6/8", IsCore = true, SkillCategoryId = 1 },
            new TagInfo { Id = 2, Name = "C#", IsCore = true, SkillCategoryId = 1 },
            new TagInfo { Id = 3, Name = "ASP.NET Core", IsCore = true, SkillCategoryId = 1 },
            new TagInfo { Id = 4, Name = "WebAPI", IsCore = true, SkillCategoryId = 1 },
            new TagInfo { Id = 5, Name = "EF Core", IsCore = true, SkillCategoryId = 1 },
            new TagInfo { Id = 6, Name = "SqlSugar", IsCore = true, SkillCategoryId = 1 },
            new TagInfo { Id = 7, Name = "Vue", IsCore = true, SkillCategoryId = 2 },
            new TagInfo { Id = 8, Name = "JavaScript", IsCore = false, SkillCategoryId = 2 },
            new TagInfo { Id = 9, Name = "WinForms", IsCore = true, SkillCategoryId = 2 },
            new TagInfo { Id = 10, Name = "WPF", IsCore = true, SkillCategoryId = 2 },
            new TagInfo { Id = 11, Name = "RS232", IsCore = true, SkillCategoryId = 3 },
            new TagInfo { Id = 12, Name = "RS485", IsCore = true, SkillCategoryId = 3 },
            new TagInfo { Id = 13, Name = "RFID", IsCore = true, SkillCategoryId = 3 },
            new TagInfo { Id = 14, Name = "TCP/IP", IsCore = true, SkillCategoryId = 3 },
            new TagInfo { Id = 15, Name = "Socket", IsCore = true, SkillCategoryId = 3 },
            new TagInfo { Id = 16, Name = "SQL Server", IsCore = true, SkillCategoryId = 4 },
            new TagInfo { Id = 17, Name = "Oracle", IsCore = true, SkillCategoryId = 4 },
            new TagInfo { Id = 18, Name = "PostgreSQL", IsCore = true, SkillCategoryId = 4 },
            new TagInfo { Id = 19, Name = "MySQL", IsCore = true, SkillCategoryId = 4 },
            new TagInfo { Id = 20, Name = "Redis", IsCore = true, SkillCategoryId = 4 },
            new TagInfo { Id = 21, Name = "RabbitMQ", IsCore = true, SkillCategoryId = 5 },
            new TagInfo { Id = 22, Name = "Docker", IsCore = true, SkillCategoryId = 5 },
            new TagInfo { Id = 23, Name = "Git", IsCore = true, SkillCategoryId = 5 },
            new TagInfo { Id = 24, Name = "Nginx", IsCore = true, SkillCategoryId = 5 },
            new TagInfo { Id = 25, Name = "CI/CD", IsCore = true, SkillCategoryId = 5 }
        );

        // 工作经历
        modelBuilder.Entity<WorkHistory>().HasData(
            new WorkHistory
            {
                Id = "work-1",
                Company = "浙江慧博科技股份有限公司",
                Role = "软件开发主管工程师",
                Period = "2025 - 至今",
                Desc = "负责PMIS、PVS、RDMS及PLM相关企业管理系统开发，参与需求分析、数据库设计、接口开发及性能优化工作。",
                IsCurrent = true
            },
            new WorkHistory
            {
                Id = "work-2",
                Company = "浙江雄伟科技开发股份有限公司",
                Role = "全栈开发工程师",
                Period = "2014 - 2025",
                Desc = "参与智慧食堂及智慧餐饮SaaS平台研发，负责业务功能开发、设备通信集成及系统实施支持。",
                IsCurrent = false
            }
        );

        // 工作成就
        modelBuilder.Entity<Achievement>().HasData(
            new Achievement { Id = 1, Description = "参与PMIS绩效管理系统功能开发与优化。", WorkHistoryId = "work-1" },
            new Achievement { Id = 2, Description = "负责PVS工艺估价系统对比物料同步及差异展示功能开发。", WorkHistoryId = "work-1" },
            new Achievement { Id = 3, Description = "完成PLM物料同步、BOM数据处理及接口开发。", WorkHistoryId = "work-1" },
            new Achievement { Id = 4, Description = "优化Oracle复杂视图及SQL查询性能，提升数据处理效率。", WorkHistoryId = "work-1" },
            new Achievement { Id = 5, Description = "独立完成需求分析、开发测试及上线部署工作。", WorkHistoryId = "work-1" },
            new Achievement { Id = 6, Description = "参与智慧食堂SaaS平台核心功能开发。", WorkHistoryId = "work-2" },
            new Achievement { Id = 7, Description = "完成RFID、刷卡终端及设备通信功能集成。", WorkHistoryId = "work-2" },
            new Achievement { Id = 8, Description = "负责餐饮消费、充值结算等业务模块开发。", WorkHistoryId = "work-2" },
            new Achievement { Id = 9, Description = "参与高并发消费场景下的数据处理与性能优化。", WorkHistoryId = "work-2" },
            new Achievement { Id = 10, Description = "完成多种硬件设备接入及现场实施支持。", WorkHistoryId = "work-2" }
        );

        // 项目经历
        modelBuilder.Entity<CompactProject>().HasData(
            new CompactProject
            {
                Id = "plm",
                Title = "PLM数据同步平台",
                Type = "企业系统",
                Year = "2025-至今",
                Desc = "负责PLM物料同步、BOM结构处理及复杂视图性能优化。"
            },
            new CompactProject
            {
                Id = "pvs",
                Title = "工艺估价系统(PVS)",
                Type = "企业系统",
                Year = "2025-至今",
                Desc = "负责对比物料同步与差异展示功能开发，实现定时同步及数据一致性校验。"
            },
            new CompactProject
            {
                Id = "rdms",
                Title = "RDMS研发管理系统",
                Type = "研发管理",
                Year = "2026-至今",
                Desc = "参与BOM管理、流程管理及研发过程数字化建设。"
            },
            new CompactProject
            {
                Id = "receipt",
                Title = "报销单扫码确认收单系统",
                Type = "独立交付",
                Year = "2025",
                Desc = "通过扫码设备完成财务单据确认收单，实现业务闭环。"
            }
        );

        // 项目技能关联
        modelBuilder.Entity<ProjectSkill>().HasData(
            new ProjectSkill { Id = 1, Name = "Oracle", ProjectId = "plm" },
            new ProjectSkill { Id = 2, Name = "SQL优化", ProjectId = "plm" },
            new ProjectSkill { Id = 3, Name = ".NET", ProjectId = "pvs" },
            new ProjectSkill { Id = 4, Name = "SqlSugar", ProjectId = "pvs" },
            new ProjectSkill { Id = 5, Name = ".NET", ProjectId = "rdms" },
            new ProjectSkill { Id = 6, Name = "Vue", ProjectId = "rdms" },
            new ProjectSkill { Id = 7, Name = "WPF", ProjectId = "receipt" },
            new ProjectSkill { Id = 8, Name = "RS485", ProjectId = "receipt" }
        );

        // 技能诊断详情
        modelBuilder.Entity<SkillDiagnostic>().HasData(
            new SkillDiagnostic
            {
                Id = 1,
                TagName = ".NET 6/8",
                Desc = "负责企业级业务系统开发，熟悉ASP.NET Core、WebAPI、依赖注入、中间件及异步编程。",
                Stat = "15 Years Experience",
                Status = "ACTIVE"
            },
            new SkillDiagnostic
            {
                Id = 2,
                TagName = "C#",
                Desc = "熟悉面向对象设计、LINQ、泛型、反射及常见设计模式应用。",
                Stat = "Advanced",
                Status = "READY"
            },
            new SkillDiagnostic
            {
                Id = 3,
                TagName = "SQL Server",
                Desc = "擅长复杂SQL编写、执行计划分析、索引优化及视图性能调优。",
                Stat = "SQL Optimization",
                Status = "ONLINE"
            },
            new SkillDiagnostic
            {
                Id = 4,
                TagName = "Oracle",
                Desc = "参与PLM及制造业系统开发，熟悉Oracle视图、存储过程及性能优化。",
                Stat = "Enterprise DB",
                Status = "ONLINE"
            },
            new SkillDiagnostic
            {
                Id = 5,
                TagName = "RabbitMQ",
                Desc = "具备消息队列开发经验，了解死信队列、延迟队列及异步解耦场景。",
                Stat = "Middleware",
                Status = "RUNNING"
            },
            new SkillDiagnostic
            {
                Id = 6,
                TagName = "RS485",
                Desc = "具备串口通信开发经验，参与扫码设备及工业设备接入项目。",
                Stat = "Device Integration",
                Status = "CONNECTED"
            },
            new SkillDiagnostic
            {
                Id = 7,
                TagName = "Blazor",
                Desc = "使用Blazor Server构建个人项目及管理系统界面。",
                Stat = "Frontend",
                Status = "ACTIVE"
            }
        );
    }
}
