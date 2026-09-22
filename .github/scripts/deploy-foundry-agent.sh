#!/usr/bin/env bash
# Publishes (creates, or publishes a new version of) one named Foundry prompt
# agent via the Foundry Agents REST API, attaching the shared geo +
# NonAI Weather MCP toolbox (wx1116-geo-nonaiweather-toolbox) with
# require_approval: never.
#
# MCP hosts remain in Foundry IaC as RemoteTool connections; the toolbox
# (deploy-foundry-toolbox.sh) wraps them and exposes a single consumer MCP
# endpoint that agents reference. This matches the portal's toolbox-first tool
# model so Tools / Used in agents render correctly.
#
# Auth: the Agents API is a data-plane operation that requires a Microsoft
# Entra ID bearer token, unlike the /openai/v1 inference endpoints. Those are
# called with AZURE_FOUNDRY_PROD_KEY only by the FoundryConsoleV1-V5 dev-tool
# consoles; api/mvc/worker authenticate to them via managed identity instead
# (Core.AIWeather.Services.FoundryTokenCredentialFactory). This script's
# caller (prod-deploy-foundry-agents.yml) logs in via azure/login and passes a
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
#   AZURE_FOUNDRY_PROD_PROJ_URL   Foundry project endpoint
#                                      (.../api/projects/<name>)
#   AZURE_FOUNDRY_ACCESS_TOKEN         Entra ID bearer token
#                                      (scope https://ai.azure.com/.default)
#   AZURE_FOUNDRY_PROD_MODEL      Model deployment name, e.g. gpt-5.4-mini
#
# Optional env: see foundry-common.sh (toolbox name/connection, injected JSON).

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
# shellcheck source=foundry-common.sh
source "${SCRIPT_DIR}/foundry-common.sh"

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

: "${AZURE_FOUNDRY_PROD_PROJ_URL:?}"
: "${AZURE_FOUNDRY_ACCESS_TOKEN:?}"
: "${AZURE_FOUNDRY_PROD_MODEL:?}"

PROJECT_ENDPOINT="$(foundry_normalize_project_endpoint "$AZURE_FOUNDRY_PROD_PROJ_URL")"
foundry_auth_headers

CREATE_URL="${PROJECT_ENDPOINT}/agents?api-version=${FOUNDRY_API_VERSION}"
VERSION_URL="${PROJECT_ENDPOINT}/agents/${AGENT_NAME}/versions?api-version=${FOUNDRY_API_VERSION}"
GET_AGENT_URL="${PROJECT_ENDPOINT}/agents/${AGENT_NAME}?api-version=${FOUNDRY_API_VERSION}"

TOOLBOX_CONSUMER_URL="$(foundry_toolbox_consumer_url "$PROJECT_ENDPOINT")"
TOOLBOX_CONNECTION_JSON="$(foundry_fetch_connection "$PROJECT_ENDPOINT" "$FOUNDRY_TOOLBOX_CONNECTION_NAME" "${FOUNDRY_TOOLBOX_CONNECTION_JSON:-}")"
TOOLBOX_CONNECTION_ID="$(foundry_connection_id <<<"$TOOLBOX_CONNECTION_JSON")"
if [ -z "$TOOLBOX_CONNECTION_ID" ]; then
  TOOLBOX_CONNECTION_ID="$FOUNDRY_TOOLBOX_CONNECTION_NAME"
fi

TOOLS_JSON="$(foundry_build_agent_toolbox_tools_json "$TOOLBOX_CONSUMER_URL" "$TOOLBOX_CONNECTION_ID")"

DEFINITION=$(jq -n \
  --arg model "$AZURE_FOUNDRY_PROD_MODEL" \
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

echo "::notice::Publishing agent '${AGENT_NAME}' (model: ${AZURE_FOUNDRY_PROD_MODEL}) to ${PROJECT_ENDPOINT}"

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
    "${FOUNDRY_AUTH_HEADERS[@]}" \
    -d "$VERSION_BODY")
else
  echo "::notice::Agent '${AGENT_NAME}' not found (GET HTTP ${GET_STATUS}); creating it."
  HTTP_STATUS=$(curl -sS -o "$RESPONSE_FILE" -w '%{http_code}' \
    -X POST "$CREATE_URL" \
    "${FOUNDRY_AUTH_HEADERS[@]}" \
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
