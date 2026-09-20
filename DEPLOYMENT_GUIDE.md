# 🚀 自动部署快速配置指南（适配现有环境）

## 📋 当前环境
- **部署路径**: `/var/www/blazor/`
- **运行方式**: 直接使用 dotnet 命令运行
- **运行用户**: root
- **访问端口**: 5000

## ⚡ 快速开始（仅需 2 步）

### 第一步：配置 GitHub Secrets

在 GitHub 仓库设置中添加（Settings → Secrets and variables → Actions → New repository secret）：

| Secret 名称 | 值 | 说明 |
|------------|-----|------|
| `ALIYUN_HOST` | 你的服务器IP | 例如：123.45.67.89 |
| `ALIYUN_USERNAME` | `root` | SSH 登录用户名 |
| `ALIYUN_SSH_KEY` | SSH 私钥完整内容 | 见下方获取方法 |
| `ALIYUN_PORT` | `22` | SSH 端口（如果没改过就是 22） |

#### 🔑 获取 SSH 私钥

**方法一：使用现有密钥**
```bash
# 查看是否已有密钥
ls -la ~/.ssh/

# 如果有 id_rsa 文件，复制其内容
cat ~/.ssh/id_rsa
```

**方法二：生成新密钥**
```bash
# 生成新的密钥对
ssh-keygen -t rsa -b 4096 -C "github-actions-deploy"

# 将公钥添加到服务器
ssh-copy-id -i ~/.ssh/id_rsa.pub root@你的服务器IP

# 复制私钥内容（包括 BEGIN 和 END 行）
cat ~/.ssh/id_rsa
```

> ⚠️ 注意：复制私钥时要包含完整内容，包括开头的 `-----BEGIN OPENSSH PRIVATE KEY-----` 和结尾的 `-----END OPENSSH PRIVATE KEY-----`

### 第二步：推送代码触发部署

```bash
git add .
git commit -m "feat: 启用自动部署"
git push origin main
```

推送后自动触发部署流程，可以在 GitHub 仓库的 **Actions** 标签页查看部署进度。

## 🔄 部署流程说明

当代码推送到 `main` 分支时，会自动执行以下步骤：

1. ✅ 在 GitHub Actions 上编译构建项目
2. ✅ 创建发布包并压缩
3. ✅ 备份服务器上的当前版本（保留最近 5 个备份）
4. ✅ 停止正在运行的应用
5. ✅ 将新版本传输到服务器
6. ✅ 启动新版本应用
7. ✅ 验证应用启动成功

## 📊 常用命令

### 查看应用状态
```bash
# 查看进程
ps aux | grep BlazorPortfolio

# 查看日志
tail -f /var/log/blazorportfolio.log
```

### 手动重启应用
```bash
# 停止应用
pkill -f "dotnet /var/www/blazor/BlazorPortfolio.dll"

# 启动应用
cd /var/www/blazor
nohup /root/.dotnet/dotnet BlazorPortfolio.dll > /var/log/blazorportfolio.log 2>&1 &
```

### 查看备份
```bash
ls -lh /var/backups/blazor/
```

### 恢复备份
```bash
# 停止应用
pkill -f "dotnet /var/www/blazor/BlazorPortfolio.dll"

# 恢复指定备份
cd /var/www/blazor
rm -rf *
tar -xzf /var/backups/blazor/backup_YYYYMMDD_HHMMSS.tar.gz

# 重启应用
nohup /root/.dotnet/dotnet BlazorPortfolio.dll > /var/log/blazorportfolio.log 2>&1 &
```

## 🎯 部署触发方式

### 自动触发
推送到 `main` 分支时自动部署：
```bash
git push origin main
```

### 手动触发
1. 进入 GitHub 仓库
2. 点击 **Actions** 标签
3. 选择 **Deploy to Aliyun Server**
4. 点击 **Run workflow**
5. 选择 `main` 分支
6. 点击 **Run workflow** 确认

## 🐛 常见问题

### 1. 部署后应用无法访问

**检查应用是否启动：**
```bash
ps aux | grep BlazorPortfolio
```

**查看错误日志：**
```bash
tail -n 50 /var/log/blazorportfolio.log
```

**手动启动应用：**
```bash
cd /var/www/blazor
/root/.dotnet/dotnet BlazorPortfolio.dll
```

### 2. GitHub Actions 部署失败

**检查 SSH 连接：**
```bash
ssh -i ~/.ssh/id_rsa root@你的服务器IP
```

**检查 Secrets 配置：**
- 确认所有 4 个 Secrets 都已正确配置
- 私钥内容完整（包括开头和结尾）
- IP 地址无误

### 3. 端口已被占用

**查看端口占用：**
```bash
netstat -tlnp | grep 5000
```

**杀死占用端口的进程：**
```bash
kill -9 $(lsof -t -i:5000)
```

## 💡 后续优化建议

### 1. 配置为 systemd 服务（推荐）

使用 systemd 管理应用更稳定，支持开机自启、自动重启等特性。

在服务器上创建服务文件：
```bash
sudo nano /etc/systemd/system/blazorportfolio.service
```

内容：
```ini
[Unit]
Description=Blazor Portfolio Application
After=network.target

[Service]
Type=notify
WorkingDirectory=/var/www/blazor
ExecStart=/root/.dotnet/dotnet /var/www/blazor/BlazorPortfolio.dll
Environment=ASPNETCORE_ENVIRONMENT=Production
Restart=always
RestartSec=10
User=root

[Install]
WantedBy=multi-user.target
```

启用服务：
```bash
sudo systemctl daemon-reload
sudo systemctl enable blazorportfolio
sudo systemctl start blazorportfolio
sudo systemctl status blazorportfolio
```

然后更新 GitHub Actions 脚本使用 systemctl 命令。

### 2. 配置 Nginx 反向代理（推荐）

使用 Nginx 可以：
- 提供更好的性能
- 支持 SSL/HTTPS
- 负载均衡
- 静态文件缓存

### 3. 配置 HTTPS

使用 Let's Encrypt 免费证书：
```bash
sudo apt install certbot python3-certbot-nginx
sudo certbot --nginx -d your-domain.com
```

### 4. 设置定时数据库备份

创建备份脚本和定时任务，定期备份 MySQL 数据库。

## 📞 需要帮助？

如果遇到问题：
1. 查看 GitHub Actions 运行日志
2. 查看服务器应用日志：`/var/log/blazorportfolio.log`
3. 检查进程状态：`ps aux | grep BlazorPortfolio`

---

✅ 现在你的项目已配置自动部署，每次推送代码到 `main` 分支都会自动更新到服务器！
