#!/bin/bash
# 服务器端测试部署脚本

APP_NAME="blazor"
DEPLOY_PATH="/var/www/$APP_NAME"
BACKUP_PATH="/var/backups/$APP_NAME"

echo "=== Step 1: Creating backup ==="
if [ -d "$DEPLOY_PATH" ]; then
  echo "Creating backup..."
  mkdir -p $BACKUP_PATH
  tar -czf $BACKUP_PATH/backup_$(date +%Y%m%d_%H%M%S).tar.gz -C $DEPLOY_PATH . 2>/dev/null || echo "Backup failed but continuing..."

  # 只保留最近5个备份
  cd $BACKUP_PATH
  ls -t 2>/dev/null | tail -n +6 | xargs -r rm 2>/dev/null || true
  echo "Backup completed"
else
  echo "No existing deployment found, skipping backup"
fi

echo "=== Step 2: Stopping application ==="
pkill -f "dotnet /var/www/blazor/BlazorPortfolio.dll" 2>/dev/null && echo "Application stopped" || echo "No running application found"
echo "Exit code from pkill: $?"
sleep 3

echo "=== Step 3: Check processes ==="
ps aux | grep -i blazor | grep -v grep

echo "=== Step 4: Test extraction ==="
echo "Checking if /tmp/deploy.tar.gz exists..."
ls -lh /tmp/deploy.tar.gz 2>/dev/null || echo "File not found (this is expected for now)"

echo "=== All steps completed ==="
echo "Script finished successfully without interruption"
