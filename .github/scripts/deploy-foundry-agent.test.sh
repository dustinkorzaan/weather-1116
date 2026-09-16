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

TOOLBOX_JSON='{"id":"conn-toolbox-id","name":"Wx1116WeatherToolbox","target":"https://acct.services.ai.azure.com/api/projects/proj/toolboxes/wx1116-weather-mcp-toolbox/mcp?api-version=v1"}'

PAYLOAD="$(
  AZURE_FOUNDRY_PROD_PROJ_URL='https://acct.services.ai.azure.com/api/projects/proj/openai/v1' \
  AZURE_FOUNDRY_ACCESS_TOKEN='test-token' \
  AZURE_FOUNDRY_PROD_MODEL='gpt-5.4-mini' \
  FOUNDRY_TOOLBOX_CONNECTION_JSON="$TOOLBOX_JSON" \
  bash "$SCRIPT" wx1116-agent-for-current-weather "$INSTRUCTIONS" \
    --response-schema "$SCHEMA" --print-body
)"

echo "$PAYLOAD" | jq empty >/dev/null || fail "print-body did not emit JSON"

CREATE_URL="$(echo "$PAYLOAD" | jq -r '.create_url')"
VERSION_URL="$(echo "$PAYLOAD" | jq -r '.version_url')"
[[ "$CREATE_URL" == 'https://acct.services.ai.azure.com/api/projects/proj/agents?api-version=v1' ]] \
  || fail "create_url should strip /openai/v1 and use /agents, got: $CREATE_URL"
[[ "$VERSION_URL" == 'https://acct.services.ai.azure.com/api/projects/proj/agents/wx1116-agent-for-current-weather/versions?api-version=v1' ]] \
  || fail "version_url mismatch: $VERSION_URL"
[[ "$CREATE_URL" != *'/assistants'* ]] || fail "must not call the Assistants API"

KIND="$(echo "$PAYLOAD" | jq -r '.create_body.definition.kind')"
[[ "$KIND" == 'prompt' ]] || fail "definition.kind should be prompt, got: $KIND"

NAME="$(echo "$PAYLOAD" | jq -r '.create_body.name')"
[[ "$NAME" == 'wx1116-agent-for-current-weather' ]] || fail "create_body.name mismatch"

echo "$PAYLOAD" | jq -e '.create_body.definition.tools | length == 1' >/dev/null \
  || fail "expected one toolbox MCP tool on the agent"
echo "$PAYLOAD" | jq -e '.create_body.definition.tools[0].require_approval == "never"' >/dev/null \
  || fail "toolbox tool require_approval should be never"
echo "$PAYLOAD" | jq -e '.create_body.definition.tools[0].project_connection_id == "conn-toolbox-id"' >/dev/null \
  || fail "agent should reference the toolbox connection id"
echo "$PAYLOAD" | jq -e '.create_body.definition.tools[0] | has("headers") | not' >/dev/null \
  || fail "MCP secrets must stay on connections, not in agent headers"
echo "$PAYLOAD" | jq -e '.create_body.definition.tools[0].server_label == "toolbox"' >/dev/null \
  || fail "toolbox server_label mismatch"
echo "$PAYLOAD" | jq -e '.create_body.definition.tools[0].server_url | endswith("/toolboxes/wx1116-weather-mcp-toolbox/mcp?api-version=v1")' >/dev/null \
  || fail "agent should point at the toolbox consumer MCP endpoint"
echo "$PAYLOAD" | jq -e '.create_body.definition.text.format.type == "json_schema"' >/dev/null \
  || fail "response schema should be definition.text.format"
echo "$PAYLOAD" | jq -e '.create_body.definition.text.format.name == "AIWeatherResponse"' >/dev/null \
  || fail "json_schema name should come from the schema file"
echo "$PAYLOAD" | jq -e 'has("create_body") and (.create_body | has("response_format") | not)' >/dev/null \
  || fail "Assistants-style top-level response_format must not be present"
echo "$PAYLOAD" | jq -e '.version_body | has("name") | not' >/dev/null \
  || fail "version body should not repeat the agent name"
echo "$PAYLOAD" | jq -e '.version_body.definition.tools[0].project_connection_id == "conn-toolbox-id"' >/dev/null \
  || fail "version body must include the same toolbox tool"

# Chat agent (no response schema) still attaches the toolbox.
CHAT_INSTRUCTIONS="${ROOT}/.github/foundry-agents/wx1116-agent-for-chat.instructions.md"
CHAT_PAYLOAD="$(
  AZURE_FOUNDRY_PROD_PROJ_URL='https://acct.services.ai.azure.com/api/projects/proj' \
  AZURE_FOUNDRY_ACCESS_TOKEN='test-token' \
  AZURE_FOUNDRY_PROD_MODEL='gpt-5.4-mini' \
  FOUNDRY_TOOLBOX_CONNECTION_JSON="$TOOLBOX_JSON" \
  bash "$SCRIPT" wx1116-agent-for-chat "$CHAT_INSTRUCTIONS" --print-body
)"
echo "$CHAT_PAYLOAD" | jq -e '.create_body.definition | has("text") | not' >/dev/null \
  || fail "chat agent must not set a JSON response schema"
echo "$CHAT_PAYLOAD" | jq -e '.create_body.definition.tools | length == 1' >/dev/null \
  || fail "chat agent should attach the toolbox MCP tool"

# Inference-only URL (no /api/projects/) must fail loudly.
if AZURE_FOUNDRY_PROD_PROJ_URL='https://acct.services.ai.azure.com/openai/v1' \
  AZURE_FOUNDRY_ACCESS_TOKEN='test-token' \
  AZURE_FOUNDRY_PROD_MODEL='gpt-5.4-mini' \
  FOUNDRY_TOOLBOX_CONNECTION_JSON="$TOOLBOX_JSON" \
  bash "$SCRIPT" wx1116-agent-for-chat "$CHAT_INSTRUCTIONS" --print-body >/dev/null 2>&1
then
  fail "openai/v1-only project URL should be rejected"
fi

echo "OK: deploy-foundry-agent.sh payload uses Foundry Agents API + toolbox MCP tool"
