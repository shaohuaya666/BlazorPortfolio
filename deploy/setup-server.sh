#!/bin/bash

# 服务器初始化脚本
# 在阿里云服务器上运行此脚本进行初始化配置

set -e

echo "========================================="
echo "BlazorPortfolio 服务器部署初始化脚本"
echo "========================================="

# 检查是否为 root 用户
if [ "$EUID" -ne 0 ]; then
    echo "请使用 root 用户或 sudo 运行此脚本"
    exit 1
fi

# 1. 安装 .NET 8.0 运行时
echo "1. 安装 .NET 8.0 运行时..."
if ! command -v dotnet &> /dev/null; then
    wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
    chmod +x dotnet-install.sh
    ./dotnet-install.sh --channel 8.0 --runtime aspnetcore
    export DOTNET_ROOT=$HOME/.dotnet
    export PATH=$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools
    echo 'export DOTNET_ROOT=$HOME/.dotnet' >> ~/.bashrc
    echo 'export PATH=$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools' >> ~/.bashrc
    rm dotnet-install.sh
    echo ".NET 安装完成"
else
    echo ".NET 已安装"
fi

# 2. 安装 Nginx
echo "2. 安装 Nginx..."
if ! command -v nginx &> /dev/null; then
    apt-get update
    apt-get install -y nginx
    echo "Nginx 安装完成"
else
    echo "Nginx 已安装"
fi

# 3. 创建应用目录
echo "3. 创建应用目录..."
mkdir -p /var/www/BlazorPortfolio
mkdir -p /var/backups/BlazorPortfolio
chown -R www-data:www-data /var/www/BlazorPortfolio
chown -R www-data:www-data /var/backups/BlazorPortfolio

# 4. 配置 systemd 服务
echo "4. 配置 systemd 服务..."
if [ -f "./blazorportfolio.service" ]; then
    cp ./blazorportfolio.service /etc/systemd/system/
    systemctl daemon-reload
    systemctl enable blazorportfolio
    echo "systemd 服务配置完成"
else
    echo "警告: blazorportfolio.service 文件不存在，请手动配置"
fi

# 5. 配置 Nginx
echo "5. 配置 Nginx..."
if [ -f "./nginx.conf" ]; then
    cp ./nginx.conf /etc/nginx/sites-available/blazorportfolio
    ln -sf /etc/nginx/sites-available/blazorportfolio /etc/nginx/sites-enabled/
    rm -f /etc/nginx/sites-enabled/default
    nginx -t && systemctl restart nginx
    echo "Nginx 配置完成"
else
    echo "警告: nginx.conf 文件不存在，请手动配置"
fi

# 6. 配置防火墙
echo "6. 配置防火墙..."
if command -v ufw &> /dev/null; then
    ufw allow 22/tcp
    ufw allow 80/tcp
    ufw allow 443/tcp
    echo "防火墙规则已添加"
fi

# 7. 显示下一步操作
echo ""
echo "========================================="
echo "初始化完成！"
echo "========================================="
echo ""
echo "下一步操作："
echo "1. 修改 nginx.conf 中的域名配置"
echo "2. 配置 appsettings.json 中的数据库连接"
echo "3. 在 GitHub 仓库中配置以下 Secrets："
echo "   - ALIYUN_HOST: 你的服务器IP或域名"
echo "   - ALIYUN_USERNAME: SSH 登录用户名（建议使用 root）"
echo "   - ALIYUN_SSH_KEY: SSH 私钥内容"
echo "   - ALIYUN_PORT: SSH 端口（默认 22）"
echo ""
echo "4. 推送代码到 main 分支，将自动触发部署"
echo ""
echo "查看应用状态: sudo systemctl status blazorportfolio"
echo "查看应用日志: sudo journalctl -u blazorportfolio -f"
echo "========================================="
