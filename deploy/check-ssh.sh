#!/bin/bash
# 在服务器上查看 SSH 公钥配置的脚本

echo "========================================="
echo "查看服务器上的 SSH 公钥"
echo "========================================="
echo ""

echo "1. 查看 authorized_keys 文件（已授权的公钥）："
echo "-------------------------------------------"
cat ~/.ssh/authorized_keys
echo ""
echo ""

echo "2. 每个公钥的指纹信息："
echo "-------------------------------------------"
ssh-keygen -lf ~/.ssh/authorized_keys
echo ""
echo ""

echo "3. authorized_keys 文件位置："
echo "-------------------------------------------"
echo "~/.ssh/authorized_keys"
echo "完整路径: $(readlink -f ~/.ssh/authorized_keys)"
echo ""

echo "========================================="
echo "注意：服务器上只能看到公钥，无法查看私钥"
echo "私钥应该保存在你的本地电脑上"
echo "========================================="
