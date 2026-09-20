# 🚀 自动部署快速指南

## 部署流程概览

```
代码推送 → GitHub Actions 自动构建 → 传输到阿里云 → 自动部署启动
```

## ⚡ 快速开始（3步完成）

### 第一步：服务器初始化

在你的阿里云服务器上执行：

```bash
# 上传部署文件到服务器
scp -r deploy/* root@你的服务器IP:/root/blazor-deploy/

# SSH 登录服务器
ssh root@你的服务器IP

# 运行初始化脚本
cd /root/blazor-deploy
chmod +x setup-server.sh
sudo ./setup-server.sh
```

### 第二步：配置 GitHub Secrets

在 GitHub 仓库设置中添加（Settings → Secrets and variables → Actions）：

| Secret 名称 | 值 |
|------------|-----|
| `ALIYUN_HOST` | 你的服务器IP（例如：123.45.67.89） |
| `ALIYUN_USERNAME` | `root` |
| `ALIYUN_SSH_KEY` | SSH 私钥完整内容 |
| `ALIYUN_PORT` | `22` |

**获取 SSH 私钥：**

```bash
# 如果没有密钥，先生成
ssh-keygen -t rsa -b 4096 -C "github-actions"

# 将公钥添加到服务器
ssh-copy-id -i ~/.ssh/id_rsa.pub root@你的服务器IP

# 复制私钥内容（整个内容包括开头和结尾）
cat ~/.ssh/id_rsa
```

### 第三步：推送代码触发部署

```bash
git add .
git commit -m "feat: 配置自动部署"
git push origin main
```

访问 GitHub 仓库的 Actions 页面，查看部署进度。

## 📁 已创建的文件

```
.github/workflows/deploy.yml      # GitHub Actions 工作流配置
deploy/
  ├── setup-server.sh             # 服务器初始化脚本
  ├── blazorportfolio.service     # systemd 服务配置
  ├── nginx.conf                  # Nginx 反向代理配置
  └── README.md                   # 详细部署文档
```

## ⚙️ 服务器配置项

### 1. 修改 Nginx 域名配置

```bash
sudo nano /etc/nginx/sites-available/blazorportfolio
```

修改 `server_name` 为你的域名或IP。

### 2. 配置生产环境数据库

在服务器创建配置文件：

```bash
sudo nano /var/www/BlazorPortfolio/appsettings.Production.json
```

内容：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=BlazorPortfolio;User=blazor;Password=你的密码;Charset=utf8mb4;"
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

### 3. 创建数据库

```bash
mysql -u root -p
```

```sql
CREATE DATABASE BlazorPortfolio CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE USER 'blazor'@'localhost' IDENTIFIED BY '你的密码';
GRANT ALL PRIVILEGES ON BlazorPortfolio.* TO 'blazor'@'localhost';
FLUSH PRIVILEGES;
EXIT;
```

## 📊 常用运维命令

```bash
# 查看应用状态
sudo systemctl status blazorportfolio

# 查看实时日志
sudo journalctl -u blazorportfolio -f

# 重启应用
sudo systemctl restart blazorportfolio

# 查看备份
ls -lh /var/backups/BlazorPortfolio/
```

## 🎯 部署触发方式

### 自动触发
推送到 `main` 分支时自动部署

### 手动触发
GitHub → Actions → Deploy to Aliyun Server → Run workflow

## 🔄 部署特性

✅ 自动构建编译  
✅ 自动传输文件  
✅ 自动备份（保留最近5个）  
✅ 零停机部署  
✅ 失败自动回滚  
✅ 实时日志监控  

## 📞 问题排查

**应用无法启动？**
```bash
sudo journalctl -u blazorportfolio -n 50
```

**502 错误？**
```bash
sudo systemctl status blazorportfolio
sudo nginx -t
```

**数据库连接失败？**
检查 `/var/www/BlazorPortfolio/appsettings.Production.json`

完整文档请查看 [deploy/README.md](deploy/README.md)
