# BlazorPortfolio

> 基于 Blazor Server 构建的个人作品集网站，展示全栈开发工程师的技术能力与项目经验。

## 📋 项目简介

BlazorPortfolio 是一个现代化的个人简历展示网站，采用 .NET 8.0 和 Blazor Server 技术栈构建。该项目以高交互性和实时响应为特点，通过 SignalR 技术实现了流畅的用户体验。数据持久层采用 **Entity Framework Core + MySQL** 实现。

### 主要特性

- **实时交互**: 基于 SignalR 的实时双向通信
- **数据库驱动**: EF Core + MySQL 持久化数据，不再硬编码
- **现代化设计**: 采用玻璃拟态（Glassmorphism）设计风格
- **响应式布局**: 支持桌面端和移动端自适应显示
- **动态内容**: 实时更新的遥测数据和诊断信息
- **技术栈展示**: 交互式技术能力诊断控制台
- **项目经历**: 详细的工作经历和项目案例展示
- **联系表单**: 内置的交互式联系表单

## 🛠️ 技术栈

- **框架**: .NET 8.0
- **前端**: Blazor Server
- **ORM**: Entity Framework Core 8.0
- **数据库**: MySQL 8.0+
- **UI**: Tailwind CSS (CDN)
- **实时通信**: SignalR
- **图标**: Material Symbols
- **编程语言**: C# 12

## 📦 项目结构

```
BlazorPortfolio/
├── Data/                   # 数据访问层
│   └── PortfolioDbContext.cs
├── Services/               # 服务层
│   └── PortfolioService.cs
├── Pages/                  # 页面组件
│   └── Index.razor         # 主页面
├── Layout/                 # 布局组件
│   └── MainLayout.razor
├── Models/                 # 数据实体模型
│   └── PortfolioData.cs
├── Hubs/                   # SignalR Hub
│   └── PortfolioHub.cs
├── Properties/             # 项目属性
│   └── launchSettings.json
├── App.razor               # 应用根组件
├── Routes.razor            # 路由配置
├── Program.cs              # 应用入口
├── _Imports.razor          # 全局引用
├── appsettings.json        # 应用配置（含数据库连接）
└── BlazorPortfolio.csproj
```

## 🚀 快速开始

### 环境要求

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) 或更高版本
- **MySQL 8.0+** 数据库
- Windows 10/11, macOS, 或 Linux

### 数据库配置

1. **安装 MySQL** 并创建数据库：

```sql
CREATE DATABASE BlazorPortfolio CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

2. **修改连接字符串**：编辑 `appsettings.json`，修改 `DefaultConnection` 中的连接信息：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=BlazorPortfolio;User=root;Password=your_password;Charset=utf8mb4;"
  }
}
```

3. **创建数据库迁移**（首次运行）：

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

> 迁移命令会自动创建所有数据表并填充种子数据。

### 运行项目

```bash
# 恢复 NuGet 包
dotnet restore

# 运行项目
dotnet run
```

### 开发模式

```bash
dotnet watch run
```

使用 `watch` 命令可以在文件修改后自动重新编译和重启应用。

## 🔧 配置说明

### 数据库连接

连接字符串配置位于 `appsettings.json` 的 `ConnectionStrings.DefaultConnection`。

### 访问来源 IP 采集（VisitTracking）

系统会把页面访问与 SignalR 连接事件的客户端 IP 写入 `ChatMessages` 表，用于定位异常连接来源。写入沿用现有"内存队列 + 后台批量持久化"通道，页面请求不直接访问数据库。

采集内容：

| 字段 | 说明 |
| --- | --- |
| `ClientIp` | 客户端真实 IP（明文，IPv4/IPv6） |
| `Timestamp` | 访问/连接时间 |
| `MessageType` | `PageView`（页面访问）/ `Connection`（连接）/ `Disconnection`（断开）/ `SystemLog` |
| `Content` | 页面访问时为请求路径（不含查询串） |
| `ConnectionId` | Hub 连接事件对应的 SignalR 连接 ID |

不记录静态资源、`/_blazor`、`/portfolioHub`、`/_framework`、健康检查与 WebSocket 升级请求。

配置项（`appsettings.json` 的 `VisitTracking` 节点，可用环境变量覆盖，如 `VisitTracking__RetentionDays=30`）：

| 配置项 | 默认值 | 说明 |
| --- | --- | --- |
| `Enabled` | `true` | 采集总开关，关闭后不再写入页面访问记录 |
| `RecordPageViews` | `true` | 是否记录页面访问（Hub 连接事件不受影响） |
| `RetentionDays` | `90` | 保留天数，`<= 0` 表示永久保留并禁用自动清理 |
| `CleanupIntervalHours` | `24` | 清理任务执行间隔（小时） |
| `CleanupBatchSize` | `1000` | 单批删除条数 |
| `MaxCleanupBatchesPerRound` | `10` | 单轮最多执行批次数 |
| `ForwardedHeadersLimit` | `2` | 信任的代理跳数 |
| `TrustedProxies` | `["127.0.0.1", "::1"]` | 受信任的代理地址，仅这些来源的转发头才会被采纳 |
| `RealIpHeader` | 未配置 | 可选的真实 IP 头（如 `CF-Connecting-IP`），仅接入 CDN 时启用 |

真实 IP 获取：

- 应用已启用 `ForwardedHeaders`（`X-Forwarded-For` + `X-Forwarded-Proto`），业务代码不手工解析转发头。
- nginx 反代需保留 `proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;`（`deploy/nginx.conf` 已包含）。
- 当 nginx 与应用不在同一台机器时，必须把 nginx 出口地址加入 `TrustedProxies`，否则取到的是代理地址。
- 切勿把 `TrustedProxies` 配置为 `0.0.0.0/0` 或过宽的网段，否则客户端可伪造 `X-Forwarded-For`。

数据库变更（已上线环境）：

```bash
# 在发布新版本之前执行，脚本幂等可重复运行
mysql -u<user> -p <database> < deploy/sql/20261104_add_chatmessage_clientip.sql
```

全新环境仍按上文 `dotnet ef migrations add InitialCreate` + `dotnet ef database update` 建库，`ClientIp` 列与索引由实体定义自动生成。

## 🔐 隐私与数据合规说明

- **采集目的**：仅用于访问量统计与异常连接来源排查（如爬虫、健康检查、重连风暴），不用于广告投放或用户画像。
- **最小化采集**：不记录 Cookie、请求体、查询串与 User-Agent；仅记录 IP、时间、路径与连接 ID。
- **明文存储风险**：IP 属于个人数据，当前按需求以明文入库。请确保数据库不对外网开放，使用最小权限账号，并限制备份文件的访问范围。
- **保留期限**：默认 90 天后由 `VisitLogCleanupService` 自动删除非业务记录；`Contact` 联系表单消息不会被清理。可用 `VisitTracking__RetentionDays` 调整。
- **随时关闭**：设置 `VisitTracking__Enabled=false` 可停止采集（需重启应用），已存数据仍按保留期清理。
- **告知义务**：面向公众部署时，建议在站点隐私说明中告知 IP 采集目的、范围与保留期限，并提供联系方式。

按 IP 查询与统计示例：

```sql
-- 最近 7 天访问量 Top 20 的 IP
SELECT ClientIp, COUNT(*) AS VisitCount, MIN(Timestamp) AS FirstSeen, MAX(Timestamp) AS LastSeen
FROM ChatMessages
WHERE MessageType = 'PageView'
  AND Timestamp >= NOW() - INTERVAL 7 DAY
GROUP BY ClientIp
ORDER BY VisitCount DESC
LIMIT 20;

-- 排查短命连接：连接后 2 秒内即断开的记录
SELECT c.ClientIp, c.Timestamp AS ConnectedAt, d.Timestamp AS DisconnectedAt
FROM ChatMessages c
JOIN ChatMessages d
  ON c.ConnectionId = d.ConnectionId
 AND c.MessageType = 'Connection'
 AND d.MessageType = 'Disconnection'
WHERE d.Timestamp <= DATE_ADD(c.Timestamp, INTERVAL 2 SECOND)
ORDER BY c.Timestamp DESC
LIMIT 50;
```

### 自定义内容

要自定义网站内容，有两种方式：

1. **直接修改数据库**：连接 MySQL 修改对应表的数据
2. **修改种子数据**：编辑 `Data/PortfolioDbContext.cs` 中的 `SeedData` 方法，重新迁移

相关数据表：
- `Advantages` - 个人优势
- `SkillCategories` / `Tags` - 技术栈分类和标签
- `WorkHistories` / `Achievements` - 工作经历和成就
- `CompactProjects` / `ProjectSkills` - 项目经历和技能
- `SkillDiagnostics` - 技能诊断详情

## 📝 功能模块

### 1. 导航栏
- 固定顶部导航
- 锚点跳转功能
- 毛玻璃背景效果

### 2. 英雄区域
- 个人定位介绍
- 实时遥测数据显示（60Hz）
- CTA 行动按钮

### 3. 个人优势
- 卡片式展示
- 悬停交互效果

### 4. 技术栈
- 分类标签展示
- 交互式诊断控制台
- 核心技能标记
- 实时能力评估

### 5. 工作经历
- 时间轴展示
- 可展开的详细成就列表
- 当前工作高亮

### 6. 项目经历
- 特色项目详细展示
- 交互式 Tab 切换
- 实时数据模拟
- 其他项目网格展示

### 7. 联系方式
- 表单提交功能
- 实时日志追踪
- 成功反馈提示

## 🎨 样式定制

项目使用内联 Tailwind CSS 类进行样式控制。主要配色方案：

- 主背景: `#051424`
- 强调色: `#4fdbc8` (青色)
- 次要色: `#adc6ff` (蓝色)
- 文本色: `#bec6e0` / `#c6c6cd`

### 自定义主题

修改 `Index.razor` 中的 CSS 类来调整主题颜色和样式。

## 📱 响应式设计

项目支持以下断点：

- `sm`: 640px
- `md`: 768px
- `lg`: 1024px
- `xl`: 1280px

## 📦 部署

### 发布到生产环境

1. **编译发布版本**
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. **部署选项**
   - IIS
   - Azure App Service
   - Docker
   - Linux 服务器 (Nginx + Kestrel)

### Docker 部署

创建 `Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["BlazorPortfolio.csproj", "./"]
RUN dotnet restore "BlazorPortfolio.csproj"
COPY . .
RUN dotnet build "BlazorPortfolio.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BlazorPortfolio.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BlazorPortfolio.dll"]
```

构建和运行：
```bash
docker build -t blazorportfolio .
docker run -d -p 8080:80 blazorportfolio
```


## 📞 联系方式

- **开发者**: 梁曦
- **技术栈**: .NET Core 8.0, Blazor Server, C# 12, EF Core, MySQL
- **项目类型**: 个人作品集网站



⚡ Built with Blazor Server & .NET 8.0 + EF Core + MySQL
