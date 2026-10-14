#!/bin/bash
# 在服务器上运行此脚本，检查是否有进程守护

echo "=== 检查 systemd 服务 ==="
systemctl list-units --type=service | grep -i blazor || echo "没有找到 systemd 服务"

echo ""
echo "=== 检查 supervisor 配置 ==="
ls -la /etc/supervisor/conf.d/ 2>/dev/null | grep -i blazor || echo "没有 supervisor 配置"

echo ""
echo "=== 检查 crontab 定时任务 ==="
crontab -l 2>/dev/null | grep -i blazor || echo "没有 crontab 任务"

echo ""
echo "=== 查看当前运行的 BlazorPortfolio 进程 ==="
ps aux | grep -i BlazorPortfolio | grep -v grep

echo ""
echo "=== 检查进程的父进程 ==="
ps -ef | grep -i BlazorPortfolio | grep -v grep
