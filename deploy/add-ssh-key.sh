#!/bin/bash
# 在阿里云服务器上执行此脚本，添加新的 SSH 公钥

echo "正在添加新的 SSH 公钥..."

# 追加新公钥到 authorized_keys（不会删除旧的）
echo "ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAACAQC76fGg+o/HqkZA7+sQPG2QQ6oRwFxzAD3N1brMh1FyWbAMDD7eTndIR7+keIiqk+sDCojMG/KNxciyecUuBjwVzqJ0LPuNow/rmXP3c8JwGqLLKRgQNtZvOYzD78101J6PFV2+iRoOGIvDDvl3FS6AGJN0c7R89j+0oWxDPA26uRnV6jZ0mYSq6bOTHYq+puon4Badim9mAssuRXBsl1PzQaHZIagBRgorc6iUqaQs2B98HAXglhF8Vs5Qoke6fLvgOA8nIx1wtmPW2UIJmrK3sVmlOoKXwqRFuiTSLTAhfHXUCoFfmmzTtMgF8zZCPLICc4+v0cPorP+m4pS2NY43iDdtSCEr+r8SCwzGHr+Qgu+XNdEtQ0HZCCpbtgqV5cg2oR77p2wrvY3E5vgDnwGw9v+R2XZlBXKoMnypkDkK9FSowZI4ywfpwzffPzVQYSZLdmwuZbAafohSqwE03Pj+XGsDQ67neyai1GzLDn3KsNG1BgrswM8DFQz0miyCpvMba9qDVrkIHBnAZEF/3B/7P9BXVk2rgwMXR1fqe6GzeH+UnfIf8NfDHNSSgkJ5UKtch1ZG0f/4zwkOaNHkmJGIuVAG6n4p96nokqxZrP3aLvyQbo6gr5kPgEytL+VmKsuIf444utD7xG47i7vUGe5Pc/sxO7LLEGNsOVkDf+LBDw== github-actions-deploy" >> ~/.ssh/authorized_keys

echo "公钥添加成功！"
echo ""
echo "当前 authorized_keys 包含的公钥数量："
grep -c "^ssh-" ~/.ssh/authorized_keys

echo ""
echo "公钥列表："
cat ~/.ssh/authorized_keys | cut -d' ' -f3
