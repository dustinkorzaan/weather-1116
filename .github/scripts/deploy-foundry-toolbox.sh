#!/usr/bin/env bash
# Publishes (creates a new version of) the shared geo + NonAI Weather Foundry
# toolbox and
# upserts the RemoteTool connection agents use to reach its consumer MCP endpoint.
#
# The toolbox wraps the two IaC-provisioned MCP RemoteTool connections
# (MyMcpSrvAppService, MyMcpSrvFuncApp). Agents attach the toolbox as a single
# MCP tool so the Foundry portal can render tool associations correctly.
#
# Usage:
#   deploy-foundry-toolbox.sh [--print-body]
#
# Required env:
#   AZURE_FOUNDRY_PROD_PROJ_URL
#   AZURE_FOUNDRY_ACCESS_TOKEN
#
# Optional env: see foundry-common.sh (toolbox name, MCP connection names, URL
# fallbacks, injected connection JSON for tests).

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
foundry_resolve_mcp_targets "$PROJECT_ENDPOINT"
foundry_auth_headers

TOOLBOX_VERSION_URL="${PROJECT_ENDPOINT}/toolboxes/${FOUNDRY_TOOLBOX_NAME}/versions?api-version=${FOUNDRY_API_VERSION}"
TOOLBOX_UPDATE_URL="${PROJECT_ENDPOINT}/toolboxes/${FOUNDRY_TOOLBOX_NAME}?api-version=${FOUNDRY_API_VERSION}"
TOOLBOX_CONNECTION_URL="${PROJECT_ENDPOINT}/connections/${FOUNDRY_TOOLBOX_CONNECTION_NAME}?api-version=${FOUNDRY_API_VERSION}"
TOOLBOX_CONSUMER_URL="$(foundry_toolbox_consumer_url "$PROJECT_ENDPOINT")"

TOOLS_JSON="$(foundry_build_weather_toolbox_tools_json)"
VERSION_BODY=$(jq -n \
  --arg description "Geo + NonAI Weather toolbox (func-app geocoding + app-service weather)" \
  --argjson tools "$TOOLS_JSON" \
  '{description: $description, tools: $tools}')

CONNECTION_BODY=$(jq -n \
  --arg target "$TOOLBOX_CONSUMER_URL" \
  '{
    category: "RemoteTool",
    target: $target,
    authType: "ProjectManagedIdentity",
    isSharedToAll: true,
    metadata: {
      type: "generic_mcp",
      audience: "https://ai.azure.com"
    }
  }')

if [ "$PRINT_BODY" -eq 1 ]; then
  jq -n \
    --arg toolbox_version_url "$TOOLBOX_VERSION_URL" \
    --arg toolbox_update_url "$TOOLBOX_UPDATE_URL" \
    --arg toolbox_connection_url "$TOOLBOX_CONNECTION_URL" \
    --arg toolbox_consumer_url "$TOOLBOX_CONSUMER_URL" \
    --argjson version_body "$VERSION_BODY" \
    --argjson connection_body "$CONNECTION_BODY" \
    '{
      toolbox_version_url: $toolbox_version_url,
      toolbox_update_url: $toolbox_update_url,
      toolbox_connection_url: $toolbox_connection_url,
      toolbox_consumer_url: $toolbox_consumer_url,
      version_body: $version_body,
      connection_body: $connection_body
    }'
  exit 0
fi

echo "::notice::Publishing toolbox '${FOUNDRY_TOOLBOX_NAME}' to ${PROJECT_ENDPOINT}"

RESPONSE_FILE="$(mktemp)"
trap 'rm -f "$RESPONSE_FILE"' EXIT

HTTP_STATUS=$(curl -sS -o "$RESPONSE_FILE" -w '%{http_code}' \
  -X POST "$TOOLBOX_VERSION_URL" \
  "${FOUNDRY_AUTH_HEADERS[@]}" \
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
  "${FOUNDRY_AUTH_HEADERS[@]}" \
  -d "$PROMOTE_BODY")

if [ "$HTTP_STATUS" -lt 200 ] || [ "$HTTP_STATUS" -ge 300 ]; then
  echo "::error::Promoting toolbox '${FOUNDRY_TOOLBOX_NAME}' version '${TOOLBOX_VERSION}' failed (HTTP ${HTTP_STATUS}). Response:" >&2
  cat "$RESPONSE_FILE" >&2
  exit 1
fi

echo "::notice::Toolbox '${FOUNDRY_TOOLBOX_NAME}' default_version set to '${TOOLBOX_VERSION}' (HTTP ${HTTP_STATUS})."

HTTP_STATUS=$(curl -sS -o "$RESPONSE_FILE" -w '%{http_code}' \
  -X PUT "$TOOLBOX_CONNECTION_URL" \
  "${FOUNDRY_AUTH_HEADERS[@]}" \
  -d "$CONNECTION_BODY")

if [ "$HTTP_STATUS" -lt 200 ] || [ "$HTTP_STATUS" -ge 300 ]; then
  echo "::error::Upserting toolbox connection '${FOUNDRY_TOOLBOX_CONNECTION_NAME}' failed (HTTP ${HTTP_STATUS}). Response:" >&2
  cat "$RESPONSE_FILE" >&2
  exit 1
fi

echo "::notice::Toolbox connection '${FOUNDRY_TOOLBOX_CONNECTION_NAME}' upserted (HTTP ${HTTP_STATUS}). Consumer endpoint: ${TOOLBOX_CONSUMER_URL}"
cat "$RESPONSE_FILE"
