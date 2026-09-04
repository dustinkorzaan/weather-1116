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
