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

## 🔄 持续集成（CI）

CI 配置位于 `.github/workflows/ci.yml`，在每次 Pull Request 以及推送 `main` / `master` 时自动运行，任一环节失败即判定检查不通过：

| 环节 | 命令 / 说明 |
| --- | --- |
| 前端依赖安装 | `npm install --no-audit --no-fund` |
| Tailwind CSS 构建 | `npm run build:css`，并校验 `wwwroot/css/app.css` 非空 |
| NuGet 依赖还原 | `dotnet restore BlazorPortfolio.sln` |
| Release 编译 | `dotnet build BlazorPortfolio.sln -c Release --no-restore` |
| 单元测试 | `dotnet test BlazorPortfolio.sln -c Release --no-build` |
| 发布 | `dotnet publish BlazorPortfolio.csproj -c Release -o publish` |
| 启动验证 | `bash scripts/ci/smoke-test.sh publish` |

启动验证以 CI 内置的 MySQL 服务为依赖运行发布产物：轮询 `/healthz` 探活，检查 Tailwind 样式产物、Razor 页面渲染与 SignalR 协商端点，最后确认进程持续存活并能优雅退出。失败时打印应用日志并上传 `ci-artifacts-<run_id>`，同时在 Job Summary 中给出各环节结果。

本地复现完整流程（需本机已启动 MySQL、Node.js）：

```bash
npm install && npm run build:css
dotnet restore && dotnet build -c Release
dotnet test -c Release --no-build
dotnet publish -c Release -o publish
# Git Bash / Linux / macOS
Kestrel__Endpoints__Http__Url=http://127.0.0.1:5099 bash scripts/ci/smoke-test.sh publish
```

### 合并保护（阻止合并）

在 GitHub 仓库 Settings → Branches 中为默认分支添加分支规则，勾选 **Require status checks to pass before merging**，并把必需检查设为 `build-test-smoke`（建议同时开启 **Require branches to be up to date before merging**）。配置完成后，检查失败或未运行的 PR 将无法合并。

> 必检状态若使用 `paths` 过滤，跳过时检查会一直处于 Pending 导致无法合并，因此工作流未做路径过滤；重复推送由 `concurrency` 自动取消旧运行。

## 🔧 配置说明

### 数据库连接

连接字符串配置位于 `appsettings.json` 的 `ConnectionStrings.DefaultConnection`。

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
