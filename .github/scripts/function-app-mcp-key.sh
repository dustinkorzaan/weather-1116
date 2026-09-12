#!/usr/bin/env bash
# Set the Function App's mcp_extension system key from the GitHub-managed
# secret, so the MCP host and its clients (api/mvc/worker, which read the same
# secret into their own app settings) always agree. GitHub is the source of
# truth; this script never generates a key.
#
# Usage: MCP_EXTENSION_KEY=<value> function-app-mcp-key.sh <app-name> <resource-group>

set -euo pipefail

APP_NAME="${1:?function app name required}"
RESOURCE_GROUP="${2:?resource group required}"
KEY_VALUE="${MCP_EXTENSION_KEY:-}"

if [ -z "$KEY_VALUE" ]; then
  cat >&2 <<'EOF'
MCP_EXTENSION_KEY is empty.
Set GitHub secret PROD_MCP_SRV_FUNC_APP_KEY to the x-functions-key value that
api/mvc/worker and Foundry should send, then re-run this deploy. Generate one
with: openssl rand -base64 32
EOF
  exit 1
fi

# `az functionapp deploy` and `config appsettings set` return once ARM
# succeeds, not once the Functions host is serving. `az functionapp keys
# list/set` talks to that host, so retry those calls until they succeed.
# ARM `state=Running` is not a readiness signal: a live app stays Running
# through zip-deploy and restarts.
STATE="$(az functionapp show \
  --name "$APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query state \
  --output tsv 2>/dev/null || true)"

if [ "$STATE" = "Stopped" ]; then
  echo "$APP_NAME state is Stopped; start it before setting keys." >&2
  exit 1
fi

WAIT_TIMEOUT_S=180
WAIT_INTERVAL_S=10
ELAPSED_S=0
CURRENT_KEY=""

while true; do
  LIST_JSON=""
  if LIST_JSON="$(az functionapp keys list \
      --name "$APP_NAME" \
      --resource-group "$RESOURCE_GROUP" \
      --output json 2>/dev/null)"; then
    CURRENT_KEY="$(jq -r '.systemKeys.mcp_extension // empty' <<< "$LIST_JSON")"
    echo "Functions host on $APP_NAME accepted keys list (elapsed ${ELAPSED_S}s)."
    break
  fi

  echo "Waiting on $APP_NAME Functions host keys API (elapsed ${ELAPSED_S}s, armState=${STATE:-unknown})..."
  if [ "$ELAPSED_S" -ge "$WAIT_TIMEOUT_S" ]; then
    echo "$APP_NAME keys list did not succeed within ${WAIT_TIMEOUT_S}s; the host is likely still restarting after deploy." >&2
    exit 1
  fi
  sleep "$WAIT_INTERVAL_S"
  ELAPSED_S=$((ELAPSED_S + WAIT_INTERVAL_S))
done

if [ "$CURRENT_KEY" = "$KEY_VALUE" ]; then
  echo "::notice::mcp_extension system key already matches PROD_MCP_SRV_FUNC_APP_KEY."
  exit 0
fi

ELAPSED_S=0
while true; do
  if az functionapp keys set \
      --name "$APP_NAME" \
      --resource-group "$RESOURCE_GROUP" \
      --key-name mcp_extension \
      --key-type systemKeys \
      --key-value "$KEY_VALUE" \
      --output none; then
    echo "::notice::Set mcp_extension system key from PROD_MCP_SRV_FUNC_APP_KEY."
    exit 0
  fi

  echo "Waiting to set mcp_extension on $APP_NAME (elapsed ${ELAPSED_S}s)..."
  if [ "$ELAPSED_S" -ge "$WAIT_TIMEOUT_S" ]; then
    echo "Failed to set mcp_extension on $APP_NAME within ${WAIT_TIMEOUT_S}s." >&2
    exit 1
  fi
  sleep "$WAIT_INTERVAL_S"
  ELAPSED_S=$((ELAPSED_S + WAIT_INTERVAL_S))
done
