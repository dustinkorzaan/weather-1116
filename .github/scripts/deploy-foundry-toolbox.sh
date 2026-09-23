#!/usr/bin/env bash
# Publishes (creates a new version of) the shared geo + NonAI Weather Foundry
# toolbox and upserts the RemoteTool connection agents use to reach its consumer
# MCP endpoint via the ARM connections API.
#
# The toolbox wraps the four IaC-provisioned MCP RemoteTool connections
# (MyMcpSrvAppService, MyMcpSrvFuncApp, MyMcpSrvPython, MyMcpSrvNode). Agents attach the
# toolbox as a single MCP tool so the Foundry portal can render tool
# associations correctly.
#
# Usage:
#   deploy-foundry-toolbox.sh [--print-body]
#
# Required env:
#   AZURE_FOUNDRY_PROD_PROJ_URL
#   AZURE_FOUNDRY_ACCESS_TOKEN
#   AZURE_FOUNDRY_ARM_ACCOUNT_NAME  ARM resource name (e.g. wx1116-prod-res), not
#                                   the data-plane hostname in PROJ_URL
#
# Optional env:
#   AZURE_SUBSCRIPTION_ID   defaults to the active az login subscription
#   AZURE_RESOURCE_GROUP      defaults to wx1116-prod-rg
#   see foundry-common.sh for toolbox/MCP connection names and test injections

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
# shellcheck source=foundry-common.sh
source "${SCRIPT_DIR}/foundry-common.sh"

PRINT_BODY=0
while [ $# -gt 0 ]; do
  case "$1" in
    --print-body)
      PRINT_BODY=1
      shift
      ;;
    *)
      echo "Unknown argument: $1" >&2
      exit 1
      ;;
  esac
done

: "${AZURE_FOUNDRY_PROD_PROJ_URL:?}"
: "${AZURE_FOUNDRY_ACCESS_TOKEN:?}"

PROJECT_ENDPOINT="$(foundry_normalize_project_endpoint "$AZURE_FOUNDRY_PROD_PROJ_URL")"
foundry_parse_project_name_from_endpoint "$PROJECT_ENDPOINT"
foundry_require_arm_account_name
foundry_resolve_mcp_targets "$PROJECT_ENDPOINT"
foundry_toolbox_auth_headers

SUBSCRIPTION_ID="${AZURE_SUBSCRIPTION_ID:-}"
if [ -z "$SUBSCRIPTION_ID" ]; then
  SUBSCRIPTION_ID="$(az account show --query id -o tsv)"
fi

TOOLBOX_VERSION_URL="${PROJECT_ENDPOINT}/toolboxes/${FOUNDRY_TOOLBOX_NAME}/versions?api-version=${FOUNDRY_API_VERSION}"
TOOLBOX_UPDATE_URL="${PROJECT_ENDPOINT}/toolboxes/${FOUNDRY_TOOLBOX_NAME}?api-version=${FOUNDRY_API_VERSION}"
TOOLBOX_CONSUMER_URL="$(foundry_toolbox_consumer_url "$PROJECT_ENDPOINT")"
ARM_CONNECTION_URL="$(foundry_arm_connection_url "$SUBSCRIPTION_ID" "$AZURE_RESOURCE_GROUP" "$FOUNDRY_ARM_ACCOUNT_NAME" "$FOUNDRY_PROJECT_NAME" "$FOUNDRY_TOOLBOX_CONNECTION_NAME")"

TOOLS_JSON="$(foundry_build_weather_toolbox_tools_json)"
VERSION_BODY=$(jq -n \
  --arg description "Geo + NonAI Weather + User toolbox (func-app/python geo + node weather + app-service user pins)" \
  --argjson tools "$TOOLS_JSON" \
  '{description: $description, tools: $tools}')

CONNECTION_PROPERTIES=$(jq -n \
  --arg target "$TOOLBOX_CONSUMER_URL" \
  '{
    category: "RemoteTool",
    target: $target,
    authType: "ProjectManagedIdentity",
    isSharedToAll: true,
    audience: "https://ai.azure.com",
    metadata: {
      type: "generic_mcp"
    }
  }')

if [ "$PRINT_BODY" -eq 1 ]; then
  jq -n \
    --arg toolbox_version_url "$TOOLBOX_VERSION_URL" \
    --arg toolbox_update_url "$TOOLBOX_UPDATE_URL" \
    --arg arm_connection_url "$ARM_CONNECTION_URL" \
    --arg toolbox_consumer_url "$TOOLBOX_CONSUMER_URL" \
    --arg arm_account_name "$FOUNDRY_ARM_ACCOUNT_NAME" \
    --arg foundry_features "$FOUNDRY_TOOLBOX_PREVIEW_FEATURE" \
    --argjson version_body "$VERSION_BODY" \
    --argjson connection_properties "$CONNECTION_PROPERTIES" \
    '{
      toolbox_version_url: $toolbox_version_url,
      toolbox_update_url: $toolbox_update_url,
      arm_connection_url: $arm_connection_url,
      toolbox_consumer_url: $toolbox_consumer_url,
      arm_account_name: $arm_account_name,
      foundry_features: $foundry_features,
      version_body: $version_body,
      connection_properties: $connection_properties
    }'
  exit 0
fi

echo "::notice::Publishing toolbox '${FOUNDRY_TOOLBOX_NAME}' to ${PROJECT_ENDPOINT}"

RESPONSE_FILE="$(mktemp)"
trap 'rm -f "$RESPONSE_FILE"' EXIT

HTTP_STATUS=$(curl -sS -o "$RESPONSE_FILE" -w '%{http_code}' \
  -X POST "$TOOLBOX_VERSION_URL" \
  "${FOUNDRY_TOOLBOX_AUTH_HEADERS[@]}" \
  -d "$VERSION_BODY")

if [ "$HTTP_STATUS" -lt 200 ] || [ "$HTTP_STATUS" -ge 300 ]; then
  echo "::error::Publishing toolbox '${FOUNDRY_TOOLBOX_NAME}' failed (HTTP ${HTTP_STATUS}). Response:" >&2
  cat "$RESPONSE_FILE" >&2
  exit 1
fi

TOOLBOX_VERSION="$(jq -r '.version // empty' "$RESPONSE_FILE")"
if [ -z "$TOOLBOX_VERSION" ]; then
  echo "::error::Toolbox version response did not include .version:" >&2
  cat "$RESPONSE_FILE" >&2
  exit 1
fi

echo "::notice::Toolbox '${FOUNDRY_TOOLBOX_NAME}' version '${TOOLBOX_VERSION}' created (HTTP ${HTTP_STATUS})."

PROMOTE_BODY=$(jq -n --arg version "$TOOLBOX_VERSION" '{default_version: $version}')
HTTP_STATUS=$(curl -sS -o "$RESPONSE_FILE" -w '%{http_code}' \
  -X PATCH "$TOOLBOX_UPDATE_URL" \
  "${FOUNDRY_TOOLBOX_AUTH_HEADERS[@]}" \
  -d "$PROMOTE_BODY")

if [ "$HTTP_STATUS" -lt 200 ] || [ "$HTTP_STATUS" -ge 300 ]; then
  echo "::error::Promoting toolbox '${FOUNDRY_TOOLBOX_NAME}' version '${TOOLBOX_VERSION}' failed (HTTP ${HTTP_STATUS}). Response:" >&2
  cat "$RESPONSE_FILE" >&2
  exit 1
fi

echo "::notice::Toolbox '${FOUNDRY_TOOLBOX_NAME}' default_version set to '${TOOLBOX_VERSION}' (HTTP ${HTTP_STATUS})."

HTTP_STATUS="$(foundry_upsert_arm_connection \
  "$SUBSCRIPTION_ID" \
  "$AZURE_RESOURCE_GROUP" \
  "$FOUNDRY_ARM_ACCOUNT_NAME" \
  "$FOUNDRY_PROJECT_NAME" \
  "$FOUNDRY_TOOLBOX_CONNECTION_NAME" \
  "$CONNECTION_PROPERTIES" \
  "$RESPONSE_FILE")"

if [ "$HTTP_STATUS" -lt 200 ] || [ "$HTTP_STATUS" -ge 300 ]; then
  echo "::error::Upserting toolbox connection '${FOUNDRY_TOOLBOX_CONNECTION_NAME}' failed (HTTP ${HTTP_STATUS}). Response:" >&2
  cat "$RESPONSE_FILE" >&2
  exit 1
fi

echo "::notice::Toolbox connection '${FOUNDRY_TOOLBOX_CONNECTION_NAME}' upserted via ARM (HTTP ${HTTP_STATUS}). Consumer endpoint: ${TOOLBOX_CONSUMER_URL}"
cat "$RESPONSE_FILE"
