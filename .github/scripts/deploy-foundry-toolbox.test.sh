#!/usr/bin/env bash
# Offline payload checks for deploy-foundry-toolbox.sh. Does not call Azure.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
SCRIPT="${ROOT}/.github/scripts/deploy-foundry-toolbox.sh"

fail() {
  echo "FAIL: $*" >&2
  exit 1
}

APP_JSON='{"id":"conn-app-id","name":"MyMcpSrvAppService","target":"https://app.example/mcp"}'
FUNC_JSON='{"id":"conn-func-id","name":"MyMcpSrvFuncApp","target":"https://func.example/runtime/webhooks/mcp"}'
PYTHON_JSON='{"id":"conn-python-id","name":"MyMcpSrvPython","target":"https://python.example/mcp"}'

PAYLOAD="$(
  AZURE_FOUNDRY_PROD_PROJ_URL='https://acct.services.ai.azure.com/api/projects/proj' \
  AZURE_FOUNDRY_ACCESS_TOKEN='test-token' \
  AZURE_SUBSCRIPTION_ID='test-sub' \
  AZURE_RESOURCE_GROUP='test-rg' \
  AZURE_FOUNDRY_ARM_ACCOUNT_NAME='wx1116-prod-res' \
  FOUNDRY_MCP_APP_CONNECTION_JSON="$APP_JSON" \
  FOUNDRY_MCP_FUNC_CONNECTION_JSON="$FUNC_JSON" \
  FOUNDRY_MCP_PYTHON_CONNECTION_JSON="$PYTHON_JSON" \
  bash "$SCRIPT" --print-body
)"

echo "$PAYLOAD" | jq empty >/dev/null || fail "print-body did not emit JSON"

VERSION_URL="$(echo "$PAYLOAD" | jq -r '.toolbox_version_url')"
UPDATE_URL="$(echo "$PAYLOAD" | jq -r '.toolbox_update_url')"
ARM_CONNECTION_URL="$(echo "$PAYLOAD" | jq -r '.arm_connection_url')"
ARM_ACCOUNT_NAME="$(echo "$PAYLOAD" | jq -r '.arm_account_name')"
CONSUMER_URL="$(echo "$PAYLOAD" | jq -r '.toolbox_consumer_url')"
FOUNDARY_FEATURES="$(echo "$PAYLOAD" | jq -r '.foundry_features')"

[[ "$VERSION_URL" == 'https://acct.services.ai.azure.com/api/projects/proj/toolboxes/wx1116-geo-nonaiweather-toolbox/versions?api-version=v1' ]] \
  || fail "toolbox_version_url mismatch: $VERSION_URL"
[[ "$UPDATE_URL" == 'https://acct.services.ai.azure.com/api/projects/proj/toolboxes/wx1116-geo-nonaiweather-toolbox?api-version=v1' ]] \
  || fail "toolbox_update_url mismatch: $UPDATE_URL"
[[ "$ARM_ACCOUNT_NAME" == 'wx1116-prod-res' ]] \
  || fail "arm_account_name should use the ARM resource name, not the data-plane subdomain: $ARM_ACCOUNT_NAME"
[[ "$ARM_CONNECTION_URL" == 'https://management.azure.com/subscriptions/test-sub/resourceGroups/test-rg/providers/Microsoft.CognitiveServices/accounts/wx1116-prod-res/projects/proj/connections/Wx1116GeoNonAIWeather?api-version=2025-04-01-preview' ]] \
  || fail "arm_connection_url mismatch: $ARM_CONNECTION_URL"
[[ "$CONSUMER_URL" == 'https://acct.services.ai.azure.com/api/projects/proj/toolboxes/wx1116-geo-nonaiweather-toolbox/mcp?api-version=v1' ]] \
  || fail "toolbox_consumer_url mismatch: $CONSUMER_URL"
[[ "$FOUNDARY_FEATURES" == 'Toolboxes=V1Preview' ]] \
  || fail "foundry_features should request toolbox preview APIs"

echo "$PAYLOAD" | jq -e '.version_body.tools | length == 3' >/dev/null \
  || fail "toolbox should wrap three MCP tools"
echo "$PAYLOAD" | jq -e '.version_body.tools[0].project_connection_id == "conn-app-id"' >/dev/null \
  || fail "toolbox app tool should reference the IaC connection id"
echo "$PAYLOAD" | jq -e '.version_body.tools[1].project_connection_id == "conn-func-id"' >/dev/null \
  || fail "toolbox func tool should reference the IaC connection id"
echo "$PAYLOAD" | jq -e '.version_body.tools[2].project_connection_id == "conn-python-id"' >/dev/null \
  || fail "toolbox python tool should reference the IaC connection id"
echo "$PAYLOAD" | jq -e '.version_body.tools[0].require_approval == "never"' >/dev/null \
  || fail "toolbox tools should set require_approval never"
echo "$PAYLOAD" | jq -e '.connection_properties.category == "RemoteTool"' >/dev/null \
  || fail "toolbox connection should be RemoteTool"
echo "$PAYLOAD" | jq -e '.connection_properties.authType == "ProjectManagedIdentity"' >/dev/null \
  || fail "toolbox connection should use project managed identity"
echo "$PAYLOAD" | jq -e '.connection_properties.target == $consumer' --arg consumer "$CONSUMER_URL" >/dev/null \
  || fail "toolbox connection target should be the consumer MCP endpoint"
echo "$PAYLOAD" | jq -e '.connection_properties.audience == "https://ai.azure.com"' >/dev/null \
  || fail "toolbox connection audience must be a top-level property for ProjectManagedIdentity token fetch"
echo "$PAYLOAD" | jq -e '.connection_properties.metadata.audience? | not' >/dev/null \
  || fail "toolbox connection audience must not be nested under metadata"

echo "OK: deploy-foundry-toolbox.sh payload wraps IaC MCP connections in a toolbox"
