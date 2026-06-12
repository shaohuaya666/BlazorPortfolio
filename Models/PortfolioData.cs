using System;
using System.Collections.Generic;

namespace BlazorPortfolio.Models
{
    public class Advantage
    {
        public string Id { get; set; } = string.Empty;
        public string Num { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Desc { get; set; } = string.Empty;
    }

    public class TagInfo
    {
        public string Name { get; set; } = string.Empty;
        public bool IsCore { get; set; }
    }

    public class SkillCategory
    {
        public string Title { get; set; } = string.Empty;
        public string ThemeColor { get; set; } = "primary"; // primary, secondary, tertiary, muted
        public List<TagInfo> Tags { get; set; } = new();
    }

    public class WorkHistory
    {
        public string Id { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Period { get; set; } = string.Empty;
        public string Desc { get; set; } = string.Empty;
        public bool IsCurrent { get; set; }
    }

    public class CompactProject
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Year { get; set; } = string.Empty;
        public string Desc { get; set; } = string.Empty;
        public List<string> Skills { get; set; } = new();
    }

    public static class PortfolioDataStore
    {
        public static readonly List<Advantage> Advantages = new()
        {
            new Advantage { Id = "adv-1", Num = "01", Title = "15年企业级开发经验", Desc = "长期从事制造业信息化系统研发，具备完整项目开发与交付经验。" },
            new Advantage { Id = "adv-2", Num = "02", Title = "业务系统设计能力", Desc = "参与PLM、PVS、RDMS等系统建设，具备业务建模与模块设计能力。" },
            new Advantage { Id = "adv-3", Num = "03", Title = "设备集成开发", Desc = "具备RS232、RS485、RFID、TCP/IP等设备接入开发经验。" },
            new Advantage { Id = "adv-4", Num = "04", Title = "SQL性能优化", Desc = "擅长复杂SQL分析、执行计划优化及大型视图性能调优。" },
            new Advantage { Id = "adv-5", Num = "05", Title = "全栈开发能力", Desc = "掌握.NET后端开发、前端页面开发及桌面端应用开发。" },
            new Advantage { Id = "adv-6", Num = "06", Title = "独立交付能力", Desc = "能够独立完成需求分析、开发测试及系统上线工作。" },
            new Advantage { Id = "adv-7", Num = "07", Title = "持续学习能力", Desc = "持续关注AI辅助开发、研发效能提升及.NET技术生态。" },
            new Advantage { Id = "adv-8", Num = "08", Title = "团队协作能力", Desc = "具备跨部门沟通经验，能够高效推进项目落地。" }
        };

        public static readonly List<SkillCategory> SkillCategories = new()
        {
            new SkillCategory
            {
                Title = "后端开发",
                ThemeColor = "tertiary",
                Tags = new()
                {
                    new TagInfo { Name = ".NET 6/8", IsCore = true },
                    new TagInfo { Name = "C#", IsCore = true },
                    new TagInfo { Name = "ASP.NET Core", IsCore = true },
                    new TagInfo { Name = "WebAPI", IsCore = true },
                    new TagInfo { Name = "EF Core", IsCore = true },
                    new TagInfo { Name = "SqlSugar", IsCore = true }
                }
            },

            new SkillCategory
            {
                Title = "前端 & 桌面端",
                ThemeColor = "secondary",
                Tags = new()
                {
                    new TagInfo { Name = "Vue", IsCore = true },
                    new TagInfo { Name = "JavaScript", IsCore = false },
                    new TagInfo { Name = "WinForms", IsCore = true },
                    new TagInfo { Name = "WPF", IsCore = true }
                }
            },

            new SkillCategory
            {
                Title = "设备通信",
                ThemeColor = "primary",
                Tags = new()
                {
                    new TagInfo { Name = "RS232", IsCore = true },
                    new TagInfo { Name = "RS485", IsCore = true },
                    new TagInfo { Name = "RFID", IsCore = true },
                    new TagInfo { Name = "TCP/IP", IsCore = true },
                    new TagInfo { Name = "Socket", IsCore = true }
                }
            },

            new SkillCategory
            {
                Title = "数据库",
                ThemeColor = "muted",
                Tags = new()
                {
                    new TagInfo { Name = "SQL Server", IsCore = true },
                    new TagInfo { Name = "Oracle", IsCore = true },
                    new TagInfo { Name = "PostgreSQL", IsCore = true },
                    new TagInfo { Name = "MySQL", IsCore = true },
                    new TagInfo { Name = "Redis", IsCore = true }
                }
            },

            new SkillCategory
            {
                Title = "中间件 & 运维",
                ThemeColor = "muted",
                Tags = new()
                {
                    new TagInfo { Name = "RabbitMQ", IsCore = true },
                    new TagInfo { Name = "Docker", IsCore = true },
                    new TagInfo { Name = "Git", IsCore = true },
                    new TagInfo { Name = "Nginx", IsCore = true },
                    new TagInfo { Name = "CI/CD", IsCore = true }
                }
            }
        };

        public static readonly List<WorkHistory> WorkHistories = new()
        {
            new WorkHistory
            {
                Id = "work-1",
                Company = "浙江慧博科技股份有限公司",
                Role = "软件开发主管工程师",
                Period = "2022 - 至今",
                Desc = "负责PMIS、PVS、RDMS及PLM相关企业管理系统开发，参与需求分析、数据库设计、接口开发及性能优化工作。",
                IsCurrent = true
            },

            new WorkHistory
            {
                Id = "work-2",
                Company = "浙江雄伟科技开发股份有限公司",
                Role = "全栈开发工程师",
                Period = "2018 - 2022",
                Desc = "参与智慧食堂及智慧餐饮SaaS平台研发，负责业务功能开发、设备通信集成及系统实施支持。",
                IsCurrent = false
            }
        };

        public static readonly List<CompactProject> CompactProjects = new()
        {
            new CompactProject
            {
                Id = "plm",
                Title = "PLM数据同步平台",
                Type = "企业系统",
                Year = "2025-至今",
                Desc = "负责PLM物料同步、BOM结构处理及复杂视图性能优化。",
                Skills = new() { "Oracle", "SQL优化" }
            },

            new CompactProject
            {
                Id = "pvs",
                Title = "工艺估价系统(PVS)",
                Type = "企业系统",
                Year = "2025-至今",
                Desc = "负责对比物料同步与差异展示功能开发，实现定时同步及数据一致性校验。",
                Skills = new() { ".NET", "SqlSugar" }
            },

            new CompactProject
            {
                Id = "rdms",
                Title = "RDMS研发管理系统",
                Type = "研发管理",
                Year = "2026-至今",
                Desc = "参与BOM管理、流程管理及研发过程数字化建设。",
                Skills = new() { ".NET", "Vue" }
            },

            new CompactProject
            {
                Id = "receipt",
                Title = "报销单扫码确认收单系统",
                Type = "独立交付",
                Year = "2025",
                Desc = "通过扫码设备完成财务单据确认收单，实现业务闭环。",
                Skills = new() { "WPF", "RS485" }
            }
        };

        public static readonly Dictionary<string, string[]> ExperienceAchievements = new()
        {
            {
                "work-1",
                new[]
                {
                    "参与PMIS绩效管理系统功能开发与优化。",
                    "负责PVS工艺估价系统对比物料同步及差异展示功能开发。",
                    "完成PLM物料同步、BOM数据处理及接口开发。",
                    "优化Oracle复杂视图及SQL查询性能，提升数据处理效率。",
                    "独立完成需求分析、开发测试及上线部署工作。"
                }
            },

            {
                "work-2",
                new[]
                {
                    "参与智慧食堂SaaS平台核心功能开发。",
                    "完成RFID、刷卡终端及设备通信功能集成。",
                    "负责餐饮消费、充值结算等业务模块开发。",
                    "参与高并发消费场景下的数据处理与性能优化。",
                    "完成多种硬件设备接入及现场实施支持。"
                }
            }
        };

        public static readonly Dictionary<string, (string Desc, string Stat, string Status)> SkillDiagnostics = new()
        {
            {
                ".NET 6/8",
                (
                    "负责企业级业务系统开发，熟悉ASP.NET Core、WebAPI、依赖注入、中间件及异步编程。",
                    "15 Years Experience",
                    "ACTIVE"
                )
            },

            {
                "C#",
                (
                    "熟悉面向对象设计、LINQ、泛型、反射及常见设计模式应用。",
                    "Advanced",
                    "READY"
                )
            },

            {
                "SQL Server",
                (
                    "擅长复杂SQL编写、执行计划分析、索引优化及视图性能调优。",
                    "SQL Optimization",
                    "ONLINE"
                )
            },

            {
                "Oracle",
                (
                    "参与PLM及制造业系统开发，熟悉Oracle视图、存储过程及性能优化。",
                    "Enterprise DB",
                    "ONLINE"
                )
            },

            {
                "RabbitMQ",
                (
                    "具备消息队列开发经验，了解死信队列、延迟队列及异步解耦场景。",
                    "Middleware",
                    "RUNNING"
                )
            },

            {
                "RS485",
                (
                    "具备串口通信开发经验，参与扫码设备及工业设备接入项目。",
                    "Device Integration",
                    "CONNECTED"
                )
            },

            {
                "Blazor",
                (
                    "使用Blazor Server构建个人项目及管理系统界面。",
                    "Frontend",
                    "ACTIVE"
                )
            }
        };
    }
}
