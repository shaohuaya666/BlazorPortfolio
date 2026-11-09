#!/usr/bin/env bash
# 启动冒烟验证：运行已发布的应用，确认进程能正常启动并对外提供关键路由。
#
# 用法：
#   bash scripts/ci/smoke-test.sh <发布目录>
#
# 可通过环境变量覆盖：
#   APP_BASE_URL        探活地址，默认 http://127.0.0.1:5099
#   STARTUP_TIMEOUT     启动等待秒数，默认 90
#   SMOKE_ARTIFACT_DIR  应用日志与响应体输出目录，默认 $RUNNER_TEMP/smoke-test
set -uo pipefail

PUBLISH_DIR="${1:-publish}"
APP_DLL="$PUBLISH_DIR/BlazorPortfolio.dll"
BASE_URL="${APP_BASE_URL:-http://127.0.0.1:5099}"
STARTUP_TIMEOUT="${STARTUP_TIMEOUT:-90}"
ARTIFACT_DIR="${SMOKE_ARTIFACT_DIR:-${RUNNER_TEMP:-/tmp}/smoke-test}"
LOG_FILE="$ARTIFACT_DIR/app.log"
BODY_FILE="$ARTIFACT_DIR/last-response.txt"
APP_PID=""

mkdir -p "$ARTIFACT_DIR"

# 判断进程是否仍在运行（排除僵尸进程，避免进程已退出却继续等待）
is_alive() {
  local stat
  stat=$(ps -o stat= -p "$1" 2>/dev/null | tr -d '[:space:]')
  [[ -n "$stat" && "$stat" != Z* ]]
}

fail() {
  echo "::error title=启动冒烟验证失败::$1"
  echo ""
  echo "----- 应用日志（最后 200 行） -----"
  tail -n 200 "$LOG_FILE" 2>/dev/null || echo "(日志文件不存在)"
  exit 1
}

cleanup() {
  if [[ -n "$APP_PID" ]] && is_alive "$APP_PID"; then
    kill "$APP_PID" 2>/dev/null || true
    for _ in $(seq 1 10); do
      is_alive "$APP_PID" || break
      sleep 1
    done
    kill -9 "$APP_PID" 2>/dev/null || true
  fi
}
trap cleanup EXIT

[[ -f "$APP_DLL" ]] || fail "未找到发布产物: $APP_DLL（请先执行 dotnet publish）"

echo "启动应用：dotnet $APP_DLL"
echo "环境：${ASPNETCORE_ENVIRONMENT:-<未设置>}，地址：$BASE_URL"

( cd "$PUBLISH_DIR" && exec dotnet BlazorPortfolio.dll ) >"$LOG_FILE" 2>&1 &
APP_PID=$!

# ---- 1. 等待进程启动并通过 /healthz 探活 ----
started=0
for ((i = 1; i <= STARTUP_TIMEOUT; i++)); do
  if ! is_alive "$APP_PID"; then
    fail "应用进程在启动后退出（约 ${i}s），请检查日志中的异常堆栈"
  fi
  if curl -fsS --max-time 2 "$BASE_URL/healthz" 2>/dev/null | grep -q '"status":"ok"'; then
    echo "✅ 启动成功：约 ${i}s 内通过 /healthz 探活"
    started=1
    break
  fi
  sleep 1
done

if [[ "$started" -ne 1 ]]; then
  fail "应用在 ${STARTUP_TIMEOUT}s 内未通过 /healthz 探活，可能卡在启动或端口未监听"
fi

# ---- 2. 关键路由检查 ----
check() {
  local name="$1" expected="$2" method="$3" url="$4" pattern="${5:-}"
  local code

  if [[ "$method" == "POST" ]]; then
    code=$(curl -sS --max-time 10 -o "$BODY_FILE" -w '%{http_code}' -X POST "$url" 2>/dev/null) || code="000"
  else
    code=$(curl -sS --max-time 10 -o "$BODY_FILE" -w '%{http_code}' "$url" 2>/dev/null) || code="000"
  fi

  [[ "$code" == "$expected" ]] || fail "$name 失败：期望 HTTP $expected，实际 $code（$url）"

  if [[ -n "$pattern" ]] && ! grep -q "$pattern" "$BODY_FILE"; then
    fail "$name 失败：响应内容未包含预期标记 $pattern（$url）"
  fi

  echo "✅ $name（HTTP $code）"
}

check "存活探针" 200 GET "$BASE_URL/healthz" '"status":"ok"'
check "Tailwind 样式产物" 200 GET "$BASE_URL/css/app.css" '.'
check "Razor 页面渲染" 200 GET "$BASE_URL/error" '<!DOCTYPE html'
check "SignalR 协商端点" 200 POST "$BASE_URL/portfolioHub/negotiate?negotiateVersion=1" 'connectionId'

# ---- 3. 进程持续存活检查 ----
sleep 3
if ! is_alive "$APP_PID"; then
  fail "应用在探活通过后意外退出，请检查日志"
fi
echo "✅ 进程保持运行"

# ---- 4. 优雅关闭（SIGTERM 后应在 15s 内退出） ----
kill "$APP_PID" 2>/dev/null || true
( sleep 15; kill -9 "$APP_PID" 2>/dev/null ) & killer_pid=$!

wait "$APP_PID" 2>/dev/null || true

if is_alive "$killer_pid"; then
  kill "$killer_pid" 2>/dev/null || true
  echo "✅ 应用已优雅停止"
else
  echo "⚠️ 应用未在 15s 内响应 SIGTERM，已被强制结束"
fi
wait "$killer_pid" 2>/dev/null || true
APP_PID=""

echo ""
echo "启动冒烟验证全部通过。应用日志：$LOG_FILE"
