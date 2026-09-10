#!/usr/bin/env bash
# Set the Functions-on-ACA mcp_extension system key from the GitHub-managed
# secret, so the MCP host and its clients (api/mvc/worker, which read the same
# secret into their own container app secrets) always agree. GitHub is the
# source of truth; this script never generates a key.
#
# Usage: MCP_EXTENSION_KEY=<value> aca-functions-mcp-key.sh <app-name> <resource-group>

set -euo pipefail

APP_NAME="${1:?container app name required}"
RESOURCE_GROUP="${2:?resource group required}"
KEY_VALUE="${MCP_EXTENSION_KEY:-}"

if [ -z "$KEY_VALUE" ]; then
  cat >&2 <<'EOF'
MCP_EXTENSION_KEY is empty.
Set GitHub secret PROD_MCP_SRV_FUNC_APP_KEY to the x-functions-key value that
api/mvc/worker and Foundry should send, then re-run this deploy. Generate one
with: openssl rand -base64 32
Docs: docs/aca-bootstrap.md
EOF
  exit 1
fi

if ! az containerapp function keys list --help >/dev/null 2>&1; then
  cat >&2 <<'EOF'
az containerapp function keys is unavailable in this Azure CLI build.
Install/update the containerapp extension, or set the mcp_extension system key
manually to the PROD_MCP_SRV_FUNC_APP_KEY value.
Docs: docs/aca-bootstrap.md
EOF
  exit 1
fi

# `az containerapp function keys` connects live to the running Functions host
# inside the newest revision rather than just PATCHing via ARM. Right after a
# deploy, that revision can still be provisioning -- az containerapp update
# returns as soon as the update is accepted, not once the revision is
# healthy -- so wait for it here or the key list/set below fails with a
# generic "Error setting function key" and no further detail.
LATEST_REVISION="$(az containerapp show \
  --name "$APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query properties.latestRevisionName \
  --output tsv)"

WAIT_TIMEOUT_S=300
WAIT_INTERVAL_S=10
ELAPSED_S=0
while true; do
  READ_STATE="$(az containerapp revision show \
    --name "$APP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --revision "$LATEST_REVISION" \
    --query "[properties.runningState, properties.healthState]" \
    --output tsv)"
  RUNNING_STATE="$(cut -f1 <<< "$READ_STATE")"
  HEALTH_STATE="$(cut -f2 <<< "$READ_STATE")"

  if [ "$RUNNING_STATE" = "Running" ] && [ "$HEALTH_STATE" = "Healthy" ]; then
    echo "::notice::Revision $LATEST_REVISION is Running/Healthy."
    break
  fi

  if [ "$ELAPSED_S" -ge "$WAIT_TIMEOUT_S" ]; then
    echo "Revision $LATEST_REVISION did not become Running/Healthy within ${WAIT_TIMEOUT_S}s (runningState=$RUNNING_STATE, healthState=$HEALTH_STATE)." >&2
    exit 1
  fi

  sleep "$WAIT_INTERVAL_S"
  ELAPSED_S=$((ELAPSED_S + WAIT_INTERVAL_S))
done

CURRENT_KEY="$(az containerapp function keys list \
  --name "$APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --key-type systemKey \
  --query "keys[?name=='mcp_extension'].value | [0]" \
  --output tsv 2>/dev/null || true)"

if [ "$CURRENT_KEY" = "$KEY_VALUE" ]; then
  echo "::notice::mcp_extension system key already matches PROD_MCP_SRV_FUNC_APP_KEY."
  exit 0
fi

# --output none keeps the key value out of the job log.
az containerapp function keys set \
  --name "$APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --key-type systemKey \
  --key-name mcp_extension \
  --key-value "$KEY_VALUE" \
  --output none

echo "::notice::Set mcp_extension system key from PROD_MCP_SRV_FUNC_APP_KEY."
