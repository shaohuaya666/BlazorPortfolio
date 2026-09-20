# BlazorPortfolio 自动部署配置指南

本文档介绍如何配置阿里云服务器的自动部署。

## 📋 前置要求

- 阿里云服务器（Ubuntu 20.04+ 或其他 Linux 发行版）
- 服务器已安装 MySQL 8.0+
- GitHub 仓库有管理员权限
- 服务器可以通过 SSH 访问

## 🚀 部署架构

```
GitHub Push → GitHub Actions → 编译构建 → SCP传输 → 阿里云服务器 → 自动部署
```

## 📦 服务器初始化（首次部署）

### 1. 上传配置文件到服务器

将 `deploy` 目录下的文件上传到服务器：

```bash
scp -r deploy/* root@your-server-ip:/root/blazor-deploy/
```

### 2. 在服务器上运行初始化脚本

```bash
ssh root@your-server-ip
cd /root/blazor-deploy
chmod +x setup-server.sh
sudo ./setup-server.sh
```

脚本会自动完成：
- 安装 .NET 8.0 运行时
- 安装并配置 Nginx
- 创建应用目录
- 配置 systemd 服务
- 设置防火墙规则

### 3. 修改配置文件

#### 修改 Nginx 配置

```bash
sudo nano /etc/nginx/sites-available/blazorportfolio
```

将 `server_name` 修改为你的域名或服务器 IP。

#### 配置应用设置

在服务器上创建生产环境配置文件：

```bash
sudo nano /var/www/BlazorPortfolio/appsettings.Production.json
```

内容：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=BlazorPortfolio;User=your_user;Password=your_password;Charset=utf8mb4;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### 4. 配置数据库

在服务器上创建数据库：

```bash
mysql -u root -p
```

```sql
CREATE DATABASE BlazorPortfolio CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE USER 'blazor'@'localhost' IDENTIFIED BY 'your_password';
GRANT ALL PRIVILEGES ON BlazorPortfolio.* TO 'blazor'@'localhost';
FLUSH PRIVILEGES;
EXIT;
```

## 🔐 GitHub Secrets 配置

在 GitHub 仓库中配置以下 Secrets：

1. 进入仓库 → Settings → Secrets and variables → Actions → New repository secret

2. 添加以下 Secrets：

| Secret 名称 | 说明 | 示例 |
|------------|------|------|
| `ALIYUN_HOST` | 服务器IP或域名 | `123.456.789.0` |
| `ALIYUN_USERNAME` | SSH 用户名 | `root` |
| `ALIYUN_SSH_KEY` | SSH 私钥完整内容 | `-----BEGIN OPENSSH PRIVATE KEY-----...` |
| `ALIYUN_PORT` | SSH 端口 | `22` |

### 获取 SSH 私钥

如果还没有 SSH 密钥对，在本地生成：

```bash
ssh-keygen -t rsa -b 4096 -C "github-actions"
```

将公钥添加到服务器：

```bash
ssh-copy-id -i ~/.ssh/id_rsa.pub root@your-server-ip
```

复制私钥内容作为 `ALIYUN_SSH_KEY`：

```bash
cat ~/.ssh/id_rsa
```

## 🎯 触发部署

### 自动部署

推送代码到 `main` 分支时自动触发：

```bash
git add .
git commit -m "feat: 新功能"
git push origin main
```

### 手动部署

在 GitHub 仓库页面：
1. Actions → Deploy to Aliyun Server
2. Run workflow → 选择 main 分支 → Run workflow

## 📊 监控和日志

### 查看应用状态

```bash
sudo systemctl status blazorportfolio
```

### 查看实时日志

```bash
sudo journalctl -u blazorportfolio -f
```

### 查看 Nginx 日志

```bash
sudo tail -f /var/log/nginx/access.log
sudo tail -f /var/log/nginx/error.log
```

### 查看最近的备份

```bash
ls -lh /var/backups/BlazorPortfolio/
```

## 🔧 常用命令

```bash
# 重启应用
sudo systemctl restart blazorportfolio

# 停止应用
sudo systemctl stop blazorportfolio

# 启动应用
sudo systemctl start blazorportfolio

# 重新加载 Nginx 配置
sudo nginx -t && sudo systemctl reload nginx

# 手动恢复备份
cd /var/www/BlazorPortfolio
sudo tar -xzf /var/backups/BlazorPortfolio/backup_YYYYMMDD_HHMMSS.tar.gz
sudo systemctl restart blazorportfolio
```

## 🐛 故障排除

### 应用无法启动

1. 检查日志：`sudo journalctl -u blazorportfolio -n 50`
2. 检查文件权限：`ls -la /var/www/BlazorPortfolio`
3. 检查数据库连接：确认 `appsettings.Production.json` 配置正确

### 502 Bad Gateway

1. 确认应用正在运行：`sudo systemctl status blazorportfolio`
2. 检查 Nginx 配置：`sudo nginx -t`
3. 查看端口占用：`sudo netstat -tlnp | grep 5000`

### GitHub Actions 部署失败

1. 检查 Secrets 是否配置正确
2. 确认 SSH 连接正常：`ssh -i ~/.ssh/id_rsa root@your-server-ip`
3. 查看 Actions 日志中的详细错误信息

## 🔄 回滚部署

如果新版本有问题，可以快速回滚：

```bash
# 查看备份列表
ls -lt /var/backups/BlazorPortfolio/

# 停止应用
sudo systemctl stop blazorportfolio

# 恢复备份
cd /var/www/BlazorPortfolio
sudo rm -rf *
sudo tar -xzf /var/backups/BlazorPortfolio/backup_YYYYMMDD_HHMMSS.tar.gz

# 重启应用
sudo systemctl start blazorportfolio
```

## 📝 最佳实践

1. **定期备份数据库**：设置定时任务备份 MySQL 数据
2. **监控磁盘空间**：备份文件会占用空间，脚本默认保留最近5个备份
3. **使用分支策略**：开发在 `dev` 分支，稳定版本合并到 `main`
4. **测试后再部署**：合并到 `main` 前确保功能测试通过
5. **配置 HTTPS**：使用 Let's Encrypt 配置 SSL 证书

## 🔒 安全建议

1. **更改默认 SSH 端口**
2. **配置防火墙规则**
3. **定期更新系统和软件包**
4. **使用强密码和密钥认证**
5. **限制 SSH 登录用户**
6. **启用应用日志审计**

## 📞 问题反馈

如有问题，请查看：
- GitHub Actions 运行日志
- 服务器应用日志
- Nginx 错误日志
- MySQL 错误日志
