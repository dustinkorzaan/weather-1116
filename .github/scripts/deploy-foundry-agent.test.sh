#!/usr/bin/env bash
# Offline payload checks for deploy-foundry-agent.sh. Does not call Azure.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
SCRIPT="${ROOT}/.github/scripts/deploy-foundry-agent.sh"
INSTRUCTIONS="${ROOT}/.github/foundry-agents/wx1116-agent-for-current-weather.instructions.md"
SCHEMA="${ROOT}/.github/foundry-agents/wx1116-agent-for-current-weather.response-schema.json"

fail() {
  echo "FAIL: $*" >&2
  exit 1
}

APP_JSON='{"id":"conn-app-id","name":"MyMcpSrvAppService","target":"https://app.example/mcp"}'
FUNC_JSON='{"id":"conn-func-id","name":"MyMcpSrvFuncApp","target":"https://func.example/runtime/webhooks/mcp"}'

PAYLOAD="$(
  AZURE_FOUNDRY_PROD_CUS_PROJ_URL='https://acct.services.ai.azure.com/api/projects/proj/openai/v1' \
  AZURE_FOUNDRY_ACCESS_TOKEN='test-token' \
  AZURE_FOUNDRY_PROD_CUS_MODEL='gpt-5.4-mini' \
  FOUNDRY_MCP_APP_CONNECTION_JSON="$APP_JSON" \
  FOUNDRY_MCP_FUNC_CONNECTION_JSON="$FUNC_JSON" \
  bash "$SCRIPT" wx1116-agent-for-current-weather "$INSTRUCTIONS" \
    --response-schema "$SCHEMA" --print-body
)"

echo "$PAYLOAD" | jq empty >/dev/null || fail "print-body did not emit JSON"

CREATE_URL="$(echo "$PAYLOAD" | jq -r '.create_url')"
VERSION_URL="$(echo "$PAYLOAD" | jq -r '.version_url')"
[[ "$CREATE_URL" == 'https://acct.services.ai.azure.com/api/projects/proj/agents?api-version=2025-11-15-preview' ]] \
  || fail "create_url should strip /openai/v1 and use /agents, got: $CREATE_URL"
[[ "$VERSION_URL" == 'https://acct.services.ai.azure.com/api/projects/proj/agents/wx1116-agent-for-current-weather/versions?api-version=2025-11-15-preview' ]] \
  || fail "version_url mismatch: $VERSION_URL"
[[ "$CREATE_URL" != *'/assistants'* ]] || fail "must not call the Assistants API"

KIND="$(echo "$PAYLOAD" | jq -r '.create_body.definition.kind')"
[[ "$KIND" == 'prompt' ]] || fail "definition.kind should be prompt, got: $KIND"

NAME="$(echo "$PAYLOAD" | jq -r '.create_body.name')"
[[ "$NAME" == 'wx1116-agent-for-current-weather' ]] || fail "create_body.name mismatch"

echo "$PAYLOAD" | jq -e '.create_body.definition.tools | length == 2' >/dev/null \
  || fail "expected two MCP tools"
echo "$PAYLOAD" | jq -e '.create_body.definition.tools[0].require_approval == "never"' >/dev/null \
  || fail "tools[0].require_approval should be never"
echo "$PAYLOAD" | jq -e '.create_body.definition.tools[1].require_approval == "never"' >/dev/null \
  || fail "tools[1].require_approval should be never"
echo "$PAYLOAD" | jq -e '.create_body.definition.tools[0].project_connection_id == "conn-app-id"' >/dev/null \
  || fail "app tool should use the IaC connection id"
echo "$PAYLOAD" | jq -e '.create_body.definition.tools[1].project_connection_id == "conn-func-id"' >/dev/null \
  || fail "func tool should use the IaC connection id"
echo "$PAYLOAD" | jq -e '.create_body.definition.tools[0] | has("headers") | not' >/dev/null \
  || fail "MCP secrets must stay on the connection, not in headers"
echo "$PAYLOAD" | jq -e '.create_body.definition.tools[0].server_label == "McpSrvAppService"' >/dev/null \
  || fail "app server_label mismatch"
echo "$PAYLOAD" | jq -e '.create_body.definition.tools[1].server_label == "McpSrvFuncApp"' >/dev/null \
  || fail "func server_label mismatch"
echo "$PAYLOAD" | jq -e '.create_body.definition.text.format.type == "json_schema"' >/dev/null \
  || fail "response schema should be definition.text.format"
echo "$PAYLOAD" | jq -e '.create_body.definition.text.format.name == "AIWeatherResponse"' >/dev/null \
  || fail "json_schema name should come from the schema file"
echo "$PAYLOAD" | jq -e 'has("create_body") and (.create_body | has("response_format") | not)' >/dev/null \
  || fail "Assistants-style top-level response_format must not be present"
echo "$PAYLOAD" | jq -e '.version_body | has("name") | not' >/dev/null \
  || fail "version body should not repeat the agent name"
echo "$PAYLOAD" | jq -e '.version_body.definition.tools[0].project_connection_id == "conn-app-id"' >/dev/null \
  || fail "version body must include the same MCP tools"

# Chat agent (no response schema) still attaches both connections.
CHAT_INSTRUCTIONS="${ROOT}/.github/foundry-agents/wx1116-agent-for-chat.instructions.md"
CHAT_PAYLOAD="$(
  AZURE_FOUNDRY_PROD_CUS_PROJ_URL='https://acct.services.ai.azure.com/api/projects/proj' \
  AZURE_FOUNDRY_ACCESS_TOKEN='test-token' \
  AZURE_FOUNDRY_PROD_CUS_MODEL='gpt-5.4-mini' \
  FOUNDRY_MCP_APP_CONNECTION_JSON="$APP_JSON" \
  FOUNDRY_MCP_FUNC_CONNECTION_JSON="$FUNC_JSON" \
  bash "$SCRIPT" wx1116-agent-for-chat "$CHAT_INSTRUCTIONS" --print-body
)"
echo "$CHAT_PAYLOAD" | jq -e '.create_body.definition | has("text") | not' >/dev/null \
  || fail "chat agent must not set a JSON response schema"
echo "$CHAT_PAYLOAD" | jq -e '.create_body.definition.tools | length == 2' >/dev/null \
  || fail "chat agent should still attach both MCP connections"

# Inference-only URL (no /api/projects/) must fail loudly.
if AZURE_FOUNDRY_PROD_CUS_PROJ_URL='https://acct.services.ai.azure.com/openai/v1' \
  AZURE_FOUNDRY_ACCESS_TOKEN='test-token' \
  AZURE_FOUNDRY_PROD_CUS_MODEL='gpt-5.4-mini' \
  FOUNDRY_MCP_APP_CONNECTION_JSON="$APP_JSON" \
  FOUNDRY_MCP_FUNC_CONNECTION_JSON="$FUNC_JSON" \
  bash "$SCRIPT" wx1116-agent-for-chat "$CHAT_INSTRUCTIONS" --print-body >/dev/null 2>&1
then
  fail "openai/v1-only project URL should be rejected"
fi

echo "OK: deploy-foundry-agent.sh payload uses Foundry Agents API + IaC MCP connections"
