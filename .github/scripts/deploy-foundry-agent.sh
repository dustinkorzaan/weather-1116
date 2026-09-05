#!/usr/bin/env bash
# Publishes (creates, or publishes a new version of) one named Foundry agent
# via the Assistants-compatible REST API, wiring in the current model
# deployment and both MCP tool connections (McpSrvAppService,
# McpSrvFuncApp) with require_approval: never.
#
# Auth: the Agents (Assistants-compatible) API is a data-plane operation
# that does not accept the Foundry account's api-key -- it requires a
# Microsoft Entra ID bearer token, unlike the /openai/v1 inference
# endpoints the rest of this app calls with AZURE_FOUNDRY_PROD_EUS2_KEY.
# (Confirmed by the HTTP 401 "invalid subscription key or wrong API
# endpoint" this script previously got when sending api-key here -- see
# Microsoft Q&A "Azure AI Agent Key Based Authentication".) The caller
# (prod-deploy-foundry-agents.yml) logs in via azure/login and passes a
# token scoped to https://ai.azure.com/.default as
# AZURE_FOUNDRY_ACCESS_TOKEN. The identity used must hold the "Foundry
# User" role (agents/*/action) at project scope -- infra/modules/ai-foundry.bicep
# grants this to the GitHub Actions identity.
#
# BEST EFFORT: the create/update contract for named, versioned Foundry
# agents (POST {project}/assistants?api-version=... creating "a new agent
# or a new version of an existing agent" when given a `name`) is the
# closest documented match found from this authoring session's available
# docs, not hand-verified against a live resource. After running this,
# check the Foundry portal (Agents -> <name> -> Versions) to confirm a new
# version actually published, and adjust API_VERSION below if Azure
# responds with an error naming a different contract.
#
# Usage:
#   deploy-foundry-agent.sh <agent-name> <instructions-file> [--response-schema <schema-file>]
#
# Required env:
#   AZURE_FOUNDRY_PROD_EUS2_PROJ_URL   Foundry project endpoint
#   AZURE_FOUNDRY_ACCESS_TOKEN         Entra ID bearer token (scope https://ai.azure.com/.default)
#   AZURE_FOUNDRY_PROD_EUS2_MODEL      Model deployment name, e.g. gpt-5.4-mini
#   MCP_SRV_APP_SERVICE_URL / MCP_SRV_APP_SERVICE_KEY
#   MCP_SRV_FUNC_APP_URL / MCP_SRV_FUNC_APP_KEY

set -euo pipefail

AGENT_NAME="${1:?agent name required}"
INSTRUCTIONS_FILE="${2:?instructions file required}"
shift 2

RESPONSE_SCHEMA_FILE=""
if [ "${1:-}" = "--response-schema" ]; then
  RESPONSE_SCHEMA_FILE="${2:?schema file required after --response-schema}"
fi

: "${AZURE_FOUNDRY_PROD_EUS2_PROJ_URL:?}"
: "${AZURE_FOUNDRY_ACCESS_TOKEN:?}"
: "${AZURE_FOUNDRY_PROD_EUS2_MODEL:?}"
: "${MCP_SRV_APP_SERVICE_URL:?}"
: "${MCP_SRV_APP_SERVICE_KEY:?}"
: "${MCP_SRV_FUNC_APP_URL:?}"
: "${MCP_SRV_FUNC_APP_KEY:?}"

API_VERSION="2025-05-01"
ENDPOINT="${AZURE_FOUNDRY_PROD_EUS2_PROJ_URL%/}/assistants?api-version=${API_VERSION}"

# Same MCP tool shape (type/server_label/server_url/headers/require_approval)
# ChatMcpToolFactory/ChatHostedMcpToolFactory already build in-process for
# Chat1b/Chat2b, and the same server_label naming (McpSrvAppService /
# McpSrvFuncApp), so the agent-hosted tools match the rest of the app.
TOOLS_JSON=$(jq -n \
  --arg appUrl "${MCP_SRV_APP_SERVICE_URL%/}/mcp" \
  --arg appKey "Bearer ${MCP_SRV_APP_SERVICE_KEY}" \
  --arg funcUrl "${MCP_SRV_FUNC_APP_URL%/}/runtime/webhooks/mcp" \
  --arg funcKey "${MCP_SRV_FUNC_APP_KEY}" \
  '[
    {
      type: "mcp",
      server_label: "McpSrvAppService",
      server_url: $appUrl,
      headers: { Authorization: $appKey },
      require_approval: "never"
    },
    {
      type: "mcp",
      server_label: "McpSrvFuncApp",
      server_url: $funcUrl,
      headers: { "x-functions-key": $funcKey },
      require_approval: "never"
    }
  ]')

BODY=$(jq -n \
  --arg name "$AGENT_NAME" \
  --arg model "$AZURE_FOUNDRY_PROD_EUS2_MODEL" \
  --rawfile instructions "$INSTRUCTIONS_FILE" \
  --argjson tools "$TOOLS_JSON" \
  '{name: $name, model: $model, instructions: $instructions, tools: $tools}')

if [ -n "$RESPONSE_SCHEMA_FILE" ]; then
  BODY=$(jq --slurpfile schema "$RESPONSE_SCHEMA_FILE" \
    '. + {response_format: {type: "json_schema", json_schema: $schema[0]}}' <<<"$BODY")
fi

echo "::notice::Publishing agent '${AGENT_NAME}' (model: ${AZURE_FOUNDRY_PROD_EUS2_MODEL}) to ${AZURE_FOUNDRY_PROD_EUS2_PROJ_URL}"

RESPONSE_FILE="$(mktemp)"
trap 'rm -f "$RESPONSE_FILE"' EXIT

HTTP_STATUS=$(curl -sS -o "$RESPONSE_FILE" -w '%{http_code}' \
  -X POST "$ENDPOINT" \
  -H "Authorization: Bearer ${AZURE_FOUNDRY_ACCESS_TOKEN}" \
  -H "Content-Type: application/json" \
  -d "$BODY")

if [ "$HTTP_STATUS" -ge 200 ] && [ "$HTTP_STATUS" -lt 300 ]; then
  echo "::notice::Agent '${AGENT_NAME}' published (HTTP ${HTTP_STATUS})."
  cat "$RESPONSE_FILE"
else
  echo "::error::Publishing agent '${AGENT_NAME}' failed (HTTP ${HTTP_STATUS}). Response:"
  cat "$RESPONSE_FILE" >&2
  exit 1
fi
