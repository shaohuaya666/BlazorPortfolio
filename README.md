# BlazorPortfolio

> 基于 Blazor Server 构建的个人作品集网站，展示全栈开发工程师的技术能力与项目经验。

## 📋 项目简介

BlazorPortfolio 是一个现代化的个人简历展示网站，采用 .NET 8.0 和 Blazor Server 技术栈构建。该项目以高交互性和实时响应为特点，通过 SignalR 技术实现了流畅的用户体验。

### 主要特性

- **实时交互**: 基于 SignalR 的实时双向通信
- **现代化设计**: 采用玻璃拟态（Glassmorphism）设计风格
- **响应式布局**: 支持桌面端和移动端自适应显示
- **动态内容**: 实时更新的遥测数据和诊断信息
- **技术栈展示**: 交互式技术能力诊断控制台
- **项目经历**: 详细的工作经历和项目案例展示
- **联系表单**: 内置的交互式联系表单

## 🛠️ 技术栈

- **框架**: .NET 8.0
- **前端**: Blazor Server
- **UI**: Tailwind CSS (内联样式)
- **实时通信**: SignalR
- **图标**: Material Symbols
- **编程语言**: C# 12

## 📦 项目结构

```
BlazorPortfolio/
├── Pages/              # 页面组件
│   └── Index.razor     # 主页面
├── Layout/             # 布局组件
│   └── MainLayout.razor
├── Models/             # 数据模型
│   └── PortfolioData.cs
├── Hubs/               # SignalR Hub
│   └── PortfolioHub.cs
├── Properties/         # 项目属性
│   └── launchSettings.json
├── App.razor           # 应用根组件
├── Routes.razor        # 路由配置
├── Program.cs          # 应用入口
├── _Imports.razor      # 全局引用
└── BlazorPortfolio.csproj
```

## 🚀 快速开始

### 环境要求

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) 或更高版本
- Windows 10/11, macOS, 或 Linux


### 开发模式

```bash
dotnet watch run
```

使用 `watch` 命令可以在文件修改后自动重新编译和重启应用。

## 🔧 配置说明

### 启动配置

项目的启动配置位于 `Properties/launchSettings.json`，可以根据需要修改端口和环境变量。

### 自定义内容

要自定义网站内容，请修改以下文件：

- `Models/PortfolioData.cs` - 个人信息、工作经历、项目经历、技术栈等数据
- `Pages/Index.razor` - 页面布局和交互逻辑
- `Layout/MainLayout.razor` - 全局布局结构

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

```

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
- **技术栈**: .NET Core 8.0, Blazor Server, C# 12
- **项目类型**: 个人作品集网站



⚡ Built with Blazor Server & .NET 8.0
