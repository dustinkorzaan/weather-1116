#!/usr/bin/env bash
# Publishes (creates, or publishes a new version of) one named Foundry prompt
# agent via the Foundry Agents REST API, attaching the two IaC-provisioned
# MCP RemoteTool connections (MyMcpSrvAppService, MyMcpSrvFuncApp) with
# require_approval: never.
#
# Tools live in Foundry IaC as project connections
# (infra/modules/ai-foundry.bicep). This script does not embed MCP secrets;
# Agent Service reads credentials from those connections at runtime.
# Foundry has no ARM resource for agents themselves, so this data-plane
# publish is the automated substitute for creating agents / attaching tools
# in the portal.
#
# Auth: the Agents API is a data-plane operation that requires a Microsoft
# Entra ID bearer token, unlike the /openai/v1 inference endpoints the rest
# of this app calls with AZURE_FOUNDRY_PROD_EUS2_KEY. The caller
# (prod-deploy-foundry-agents.yml) logs in via azure/login and passes a
# token scoped to https://ai.azure.com/.default as
# AZURE_FOUNDRY_ACCESS_TOKEN. The identity used must hold the "Foundry
# User" role (agents/*/action) at project scope -- infra/modules/ai-foundry.bicep
# grants this to the GitHub Actions identity.
#
# Usage:
#   deploy-foundry-agent.sh <agent-name> <instructions-file> \
#     [--response-schema <schema-file>] [--print-body]
#
# Required env:
#   AZURE_FOUNDRY_PROD_EUS2_PROJ_URL   Foundry project endpoint
#                                      (.../api/projects/<name>)
#   AZURE_FOUNDRY_ACCESS_TOKEN         Entra ID bearer token
#                                      (scope https://ai.azure.com/.default)
#   AZURE_FOUNDRY_PROD_EUS2_MODEL      Model deployment name, e.g. gpt-5.4-mini
#
# Optional env:
#   FOUNDRY_MCP_APP_CONNECTION_NAME    default MyMcpSrvAppService
#   FOUNDRY_MCP_FUNC_CONNECTION_NAME   default MyMcpSrvFuncApp
#   MCP_SRV_APP_SERVICE_URL / MCP_SRV_FUNC_APP_URL
#     Base URLs used only when a connection GET does not return target.
#   FOUNDRY_MCP_APP_CONNECTION_JSON / FOUNDRY_MCP_FUNC_CONNECTION_JSON
#     Injected connection payloads for tests (skip live GET).

set -euo pipefail

AGENT_NAME="${1:?agent name required}"
INSTRUCTIONS_FILE="${2:?instructions file required}"
shift 2

RESPONSE_SCHEMA_FILE=""
PRINT_BODY=0
while [ $# -gt 0 ]; do
  case "$1" in
    --response-schema)
      RESPONSE_SCHEMA_FILE="${2:?schema file required after --response-schema}"
      shift 2
      ;;
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

: "${AZURE_FOUNDRY_PROD_EUS2_PROJ_URL:?}"
: "${AZURE_FOUNDRY_ACCESS_TOKEN:?}"
: "${AZURE_FOUNDRY_PROD_EUS2_MODEL:?}"

FOUNDRY_MCP_APP_CONNECTION_NAME="${FOUNDRY_MCP_APP_CONNECTION_NAME:-MyMcpSrvAppService}"
FOUNDRY_MCP_FUNC_CONNECTION_NAME="${FOUNDRY_MCP_FUNC_CONNECTION_NAME:-MyMcpSrvFuncApp}"

API_VERSION="v1"

normalize_project_endpoint() {
  local url="${1%/}"
  url="${url%/openai/v1}"
  if [[ "$url" != *"/api/projects/"* ]]; then
    echo "AZURE_FOUNDRY_PROD_EUS2_PROJ_URL must be a Foundry project endpoint (https://<account>.services.ai.azure.com/api/projects/<name>), not an OpenAI inference URL." >&2
    exit 1
  fi
  printf '%s' "$url"
}

PROJECT_ENDPOINT="$(normalize_project_endpoint "$AZURE_FOUNDRY_PROD_EUS2_PROJ_URL")"
CREATE_URL="${PROJECT_ENDPOINT}/agents?api-version=${API_VERSION}"
VERSION_URL="${PROJECT_ENDPOINT}/agents/${AGENT_NAME}/versions?api-version=${API_VERSION}"
GET_AGENT_URL="${PROJECT_ENDPOINT}/agents/${AGENT_NAME}?api-version=${API_VERSION}"

auth_headers=(
  -H "Authorization: Bearer ${AZURE_FOUNDRY_ACCESS_TOKEN}"
  -H "Content-Type: application/json"
)

fetch_connection() {
  local name="$1"
  local injected_json="${2:-}"
  local url="${PROJECT_ENDPOINT}/connections/${name}?api-version=${API_VERSION}"
  local response_file http_status

  if [ -n "$injected_json" ]; then
    printf '%s' "$injected_json"
    return
  fi

  response_file="$(mktemp)"
  http_status=$(curl -sS -o "$response_file" -w '%{http_code}' \
    -X GET "$url" \
    -H "Authorization: Bearer ${AZURE_FOUNDRY_ACCESS_TOKEN}" \
    -H "Content-Type: application/json") || true

  if [ "$http_status" -ge 200 ] && [ "$http_status" -lt 300 ]; then
    cat "$response_file"
    rm -f "$response_file"
    return
  fi

  echo "::warning::GET ${url} failed (HTTP ${http_status}). Using connection name '${name}' as project_connection_id. Response:" >&2
  cat "$response_file" >&2 || true
  rm -f "$response_file"
  jq -n --arg name "$name" '{name: $name}'
}

connection_id() {
  jq -r '.id // .name // empty'
}

connection_target() {
  jq -r '.target // empty'
}

APP_CONNECTION_JSON="$(fetch_connection "$FOUNDRY_MCP_APP_CONNECTION_NAME" "${FOUNDRY_MCP_APP_CONNECTION_JSON:-}")"
FUNC_CONNECTION_JSON="$(fetch_connection "$FOUNDRY_MCP_FUNC_CONNECTION_NAME" "${FOUNDRY_MCP_FUNC_CONNECTION_JSON:-}")"

APP_CONNECTION_ID="$(connection_id <<<"$APP_CONNECTION_JSON")"
FUNC_CONNECTION_ID="$(connection_id <<<"$FUNC_CONNECTION_JSON")"
APP_TARGET="$(connection_target <<<"$APP_CONNECTION_JSON")"
FUNC_TARGET="$(connection_target <<<"$FUNC_CONNECTION_JSON")"

if [ -z "$APP_CONNECTION_ID" ]; then
  APP_CONNECTION_ID="$FOUNDRY_MCP_APP_CONNECTION_NAME"
fi
if [ -z "$FUNC_CONNECTION_ID" ]; then
  FUNC_CONNECTION_ID="$FOUNDRY_MCP_FUNC_CONNECTION_NAME"
fi

if [ -z "$APP_TARGET" ]; then
  : "${MCP_SRV_APP_SERVICE_URL:?MCP app-service URL required when the Foundry connection has no target}"
  APP_TARGET="${MCP_SRV_APP_SERVICE_URL%/}/mcp"
fi
if [ -z "$FUNC_TARGET" ]; then
  : "${MCP_SRV_FUNC_APP_URL:?MCP func-app URL required when the Foundry connection has no target}"
  FUNC_TARGET="${MCP_SRV_FUNC_APP_URL%/}/runtime/webhooks/mcp"
fi

# MCP tool shape for the Foundry Agents API (PromptAgentDefinition), not the
# Assistants-compatible /assistants contract. require_approval is valid here.
# Secrets stay on the RemoteTool connections; do not send headers.
TOOLS_JSON=$(jq -n \
  --arg appLabel "McpSrvAppService" \
  --arg appUrl "$APP_TARGET" \
  --arg appConn "$APP_CONNECTION_ID" \
  --arg funcLabel "McpSrvFuncApp" \
  --arg funcUrl "$FUNC_TARGET" \
  --arg funcConn "$FUNC_CONNECTION_ID" \
  '[
    {
      type: "mcp",
      server_label: $appLabel,
      server_url: $appUrl,
      project_connection_id: $appConn,
      require_approval: "never"
    },
    {
      type: "mcp",
      server_label: $funcLabel,
      server_url: $funcUrl,
      project_connection_id: $funcConn,
      require_approval: "never"
    }
  ]')

DEFINITION=$(jq -n \
  --arg model "$AZURE_FOUNDRY_PROD_EUS2_MODEL" \
  --rawfile instructions "$INSTRUCTIONS_FILE" \
  --argjson tools "$TOOLS_JSON" \
  '{kind: "prompt", model: $model, instructions: $instructions, tools: $tools}')

if [ -n "$RESPONSE_SCHEMA_FILE" ]; then
  DEFINITION=$(jq --slurpfile schema "$RESPONSE_SCHEMA_FILE" \
    '. + {text: {format: ($schema[0] + {type: "json_schema"})}}' <<<"$DEFINITION")
fi

CREATE_BODY=$(jq -n \
  --arg name "$AGENT_NAME" \
  --argjson definition "$DEFINITION" \
  '{name: $name, definition: $definition}')

VERSION_BODY=$(jq -n \
  --argjson definition "$DEFINITION" \
  '{definition: $definition}')

if [ "$PRINT_BODY" -eq 1 ]; then
  jq -n \
    --arg create_url "$CREATE_URL" \
    --arg version_url "$VERSION_URL" \
    --argjson create_body "$CREATE_BODY" \
    --argjson version_body "$VERSION_BODY" \
    '{create_url: $create_url, version_url: $version_url, create_body: $create_body, version_body: $version_body}'
  exit 0
fi

echo "::notice::Publishing agent '${AGENT_NAME}' (model: ${AZURE_FOUNDRY_PROD_EUS2_MODEL}) to ${PROJECT_ENDPOINT}"

RESPONSE_FILE="$(mktemp)"
trap 'rm -f "$RESPONSE_FILE"' EXIT

agent_exists=0
GET_STATUS=$(curl -sS -o "$RESPONSE_FILE" -w '%{http_code}' \
  -X GET "$GET_AGENT_URL" \
  -H "Authorization: Bearer ${AZURE_FOUNDRY_ACCESS_TOKEN}") || true
if [ "$GET_STATUS" -ge 200 ] && [ "$GET_STATUS" -lt 300 ]; then
  agent_exists=1
fi

if [ "$agent_exists" -eq 1 ]; then
  echo "::notice::Agent '${AGENT_NAME}' exists; publishing a new version."
  HTTP_STATUS=$(curl -sS -o "$RESPONSE_FILE" -w '%{http_code}' \
    -X POST "$VERSION_URL" \
    "${auth_headers[@]}" \
    -d "$VERSION_BODY")
else
  echo "::notice::Agent '${AGENT_NAME}' not found (GET HTTP ${GET_STATUS}); creating it."
  HTTP_STATUS=$(curl -sS -o "$RESPONSE_FILE" -w '%{http_code}' \
    -X POST "$CREATE_URL" \
    "${auth_headers[@]}" \
    -d "$CREATE_BODY")
fi

if [ "$HTTP_STATUS" -ge 200 ] && [ "$HTTP_STATUS" -lt 300 ]; then
  echo "::notice::Agent '${AGENT_NAME}' published (HTTP ${HTTP_STATUS})."
  cat "$RESPONSE_FILE"
else
  echo "::error::Publishing agent '${AGENT_NAME}' failed (HTTP ${HTTP_STATUS}). Response:"
  cat "$RESPONSE_FILE" >&2
  exit 1
fi
