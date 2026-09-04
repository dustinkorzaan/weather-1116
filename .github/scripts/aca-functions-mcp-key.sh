#!/usr/bin/env bash
# Ensure the Functions-on-ACA mcp_extension system key exists when supported by
# the installed Azure CLI. Exits non-zero with guidance when the command group is
# missing so deploy does not silently skip MCP auth setup.
#
# Usage: aca-functions-mcp-key.sh <app-name> <resource-group>

set -euo pipefail

APP_NAME="${1:?container app name required}"
RESOURCE_GROUP="${2:?resource group required}"

if ! az containerapp function keys list --help >/dev/null 2>&1; then
  cat >&2 <<EOF
az containerapp function keys is unavailable in this Azure CLI build.
Install/update the containerapp extension, or create the mcp_extension system key
manually and set GitHub secret PROD_MCP_SRV_FUNC_APP_KEY.
Docs: docs/aca-bootstrap.md
EOF
  exit 1
fi

KEY_VALUE="$(az containerapp function keys list \
  --name "$APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --key-type systemKey \
  --query "keys[?name=='mcp_extension'].value | [0]" \
  --output tsv 2>/dev/null || true)"

if [ -z "$KEY_VALUE" ] || [ "$KEY_VALUE" = "None" ]; then
  KEY_VALUE="$(openssl rand -base64 32)"
  az containerapp function keys set \
    --name "$APP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --key-type systemKey \
    --key-name mcp_extension \
    --key-value "$KEY_VALUE"
  echo "::notice::Created mcp_extension system key. Update PROD_MCP_SRV_FUNC_APP_KEY after first deploy."
else
  echo "::notice::mcp_extension system key already present."
fi
