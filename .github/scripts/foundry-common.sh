#!/usr/bin/env bash
# Shared helpers for Foundry data-plane deploy scripts (toolbox + agents).
set -euo pipefail

FOUNDRY_API_VERSION="${FOUNDRY_API_VERSION:-v1}"

FOUNDRY_MCP_APP_CONNECTION_NAME="${FOUNDRY_MCP_APP_CONNECTION_NAME:-MyMcpSrvAppService}"
FOUNDRY_MCP_FUNC_CONNECTION_NAME="${FOUNDRY_MCP_FUNC_CONNECTION_NAME:-MyMcpSrvFuncApp}"
FOUNDRY_TOOLBOX_NAME="${FOUNDRY_TOOLBOX_NAME:-wx1116-weather-mcp-toolbox}"
FOUNDRY_TOOLBOX_CONNECTION_NAME="${FOUNDRY_TOOLBOX_CONNECTION_NAME:-Wx1116WeatherToolbox}"

foundry_normalize_project_endpoint() {
  local url="${1%/}"
  url="${url%/openai/v1}"
  if [[ "$url" != *"/api/projects/"* ]]; then
    echo "AZURE_FOUNDRY_PROD_PROJ_URL must be a Foundry project endpoint (https://<account>.services.ai.azure.com/api/projects/<name>), not an OpenAI inference URL." >&2
    exit 1
  fi
  printf '%s' "$url"
}

foundry_auth_headers() {
  FOUNDRY_AUTH_HEADERS=(
    -H "Authorization: Bearer ${AZURE_FOUNDRY_ACCESS_TOKEN}"
    -H "Content-Type: application/json"
  )
}

foundry_fetch_connection() {
  local project_endpoint="$1"
  local name="$2"
  local injected_json="${3:-}"
  local url="${project_endpoint}/connections/${name}?api-version=${FOUNDRY_API_VERSION}"
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

foundry_connection_id() {
  jq -r '.id // .name // empty'
}

foundry_connection_target() {
  jq -r '.target // empty'
}

foundry_resolve_mcp_targets() {
  local project_endpoint="$1"

  local app_json func_json
  app_json="$(foundry_fetch_connection "$project_endpoint" "$FOUNDRY_MCP_APP_CONNECTION_NAME" "${FOUNDRY_MCP_APP_CONNECTION_JSON:-}")"
  func_json="$(foundry_fetch_connection "$project_endpoint" "$FOUNDRY_MCP_FUNC_CONNECTION_NAME" "${FOUNDRY_MCP_FUNC_CONNECTION_JSON:-}")"

  FOUNDRY_MCP_APP_CONNECTION_ID="$(foundry_connection_id <<<"$app_json")"
  FOUNDRY_MCP_FUNC_CONNECTION_ID="$(foundry_connection_id <<<"$func_json")"
  FOUNDRY_MCP_APP_TARGET="$(foundry_connection_target <<<"$app_json")"
  FOUNDRY_MCP_FUNC_TARGET="$(foundry_connection_target <<<"$func_json")"

  if [ -z "$FOUNDRY_MCP_APP_CONNECTION_ID" ]; then
    FOUNDRY_MCP_APP_CONNECTION_ID="$FOUNDRY_MCP_APP_CONNECTION_NAME"
  fi
  if [ -z "$FOUNDRY_MCP_FUNC_CONNECTION_ID" ]; then
    FOUNDRY_MCP_FUNC_CONNECTION_ID="$FOUNDRY_MCP_FUNC_CONNECTION_NAME"
  fi

  if [ -z "$FOUNDRY_MCP_APP_TARGET" ]; then
    : "${MCP_SRV_APP_SERVICE_URL:?MCP app-service URL required when the Foundry connection has no target}"
    FOUNDRY_MCP_APP_TARGET="${MCP_SRV_APP_SERVICE_URL%/}/mcp"
  fi
  if [ -z "$FOUNDRY_MCP_FUNC_TARGET" ]; then
    : "${MCP_SRV_FUNC_APP_URL:?MCP func-app URL required when the Foundry connection has no target}"
    FOUNDRY_MCP_FUNC_TARGET="${MCP_SRV_FUNC_APP_URL%/}/runtime/webhooks/mcp"
  fi
}

foundry_toolbox_consumer_url() {
  local project_endpoint="$1"
  printf '%s/toolboxes/%s/mcp?api-version=%s' \
    "$project_endpoint" "$FOUNDRY_TOOLBOX_NAME" "$FOUNDRY_API_VERSION"
}

foundry_build_weather_toolbox_tools_json() {
  jq -n \
    --arg appLabel "McpSrvAppService" \
    --arg appUrl "$FOUNDRY_MCP_APP_TARGET" \
    --arg appConn "$FOUNDRY_MCP_APP_CONNECTION_ID" \
    --arg funcLabel "McpSrvFuncApp" \
    --arg funcUrl "$FOUNDRY_MCP_FUNC_TARGET" \
    --arg funcConn "$FOUNDRY_MCP_FUNC_CONNECTION_ID" \
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
    ]'
}

foundry_build_agent_toolbox_tools_json() {
  local toolbox_url="$1"
  local toolbox_connection_id="$2"

  jq -n \
    --arg label "toolbox" \
    --arg url "$toolbox_url" \
    --arg conn "$toolbox_connection_id" \
    '[
      {
        type: "mcp",
        server_label: $label,
        server_url: $url,
        project_connection_id: $conn,
        require_approval: "never"
      }
    ]'
}
