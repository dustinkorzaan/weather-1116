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

# `az functionapp deploy` returns once the zip-deploy operation succeeds, not
# once the app has actually restarted with it -- wait for the app to report
# Running or the key list/set below can hit a still-restarting host.
WAIT_TIMEOUT_S=180
WAIT_INTERVAL_S=10
ELAPSED_S=0
while true; do
  STATE="$(az functionapp show \
    --name "$APP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --query state \
    --output tsv 2>/dev/null || true)"

  echo "Waiting on $APP_NAME (state=$STATE)..."

  if [ "$STATE" = "Running" ]; then
    echo "::notice::$APP_NAME is Running."
    break
  fi

  if [ "$ELAPSED_S" -ge "$WAIT_TIMEOUT_S" ]; then
    echo "$APP_NAME did not report state=Running within ${WAIT_TIMEOUT_S}s (state=$STATE)." >&2
    exit 1
  fi

  sleep "$WAIT_INTERVAL_S"
  ELAPSED_S=$((ELAPSED_S + WAIT_INTERVAL_S))
done

CURRENT_KEY="$(az functionapp keys list \
  --name "$APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query "systemKeys.mcp_extension" \
  --output tsv 2>/dev/null || true)"

if [ "$CURRENT_KEY" = "$KEY_VALUE" ]; then
  echo "::notice::mcp_extension system key already matches PROD_MCP_SRV_FUNC_APP_KEY."
  exit 0
fi

# --output none keeps the key value out of the job log.
az functionapp keys set \
  --name "$APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --key-name mcp_extension \
  --key-type systemKeys \
  --key-value "$KEY_VALUE" \
  --output none

echo "::notice::Set mcp_extension system key from PROD_MCP_SRV_FUNC_APP_KEY."
