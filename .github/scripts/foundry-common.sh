#!/usr/bin/env bash
# Shared helpers for Foundry data-plane deploy scripts (toolbox + agents).
set -euo pipefail

FOUNDRY_API_VERSION="${FOUNDRY_API_VERSION:-v1}"
FOUNDRY_ARM_CONNECTION_API_VERSION="${FOUNDRY_ARM_CONNECTION_API_VERSION:-2025-04-01-preview}"
FOUNDRY_TOOLBOX_PREVIEW_FEATURE="${FOUNDRY_TOOLBOX_PREVIEW_FEATURE:-Toolboxes=V1Preview}"
AZURE_RESOURCE_GROUP="${AZURE_RESOURCE_GROUP:-wx1116-prod-rg}"

FOUNDRY_MCP_APP_CONNECTION_NAME="${FOUNDRY_MCP_APP_CONNECTION_NAME:-MyMcpSrvAppService}"
FOUNDRY_MCP_FUNC_CONNECTION_NAME="${FOUNDRY_MCP_FUNC_CONNECTION_NAME:-MyMcpSrvFuncApp}"
FOUNDRY_MCP_PYTHON_CONNECTION_NAME="${FOUNDRY_MCP_PYTHON_CONNECTION_NAME:-MyMcpSrvPython}"
FOUNDRY_MCP_NODE_CONNECTION_NAME="${FOUNDRY_MCP_NODE_CONNECTION_NAME:-MyMcpSrvNode}"
FOUNDRY_TOOLBOX_NAME="${FOUNDRY_TOOLBOX_NAME:-wx1116-geo-nonaiweather-toolbox}"
FOUNDRY_TOOLBOX_CONNECTION_NAME="${FOUNDRY_TOOLBOX_CONNECTION_NAME:-Wx1116GeoNonAIWeather}"

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

foundry_toolbox_auth_headers() {
  foundry_auth_headers
  FOUNDRY_TOOLBOX_AUTH_HEADERS=(
    "${FOUNDRY_AUTH_HEADERS[@]}"
    -H "Foundry-Features: ${FOUNDRY_TOOLBOX_PREVIEW_FEATURE}"
  )
}

foundry_parse_project_name_from_endpoint() {
  local endpoint="$1"
  if [[ "$endpoint" =~ ^https://[^./]+\.services\.ai\.azure\.com/api/projects/([^/]+) ]]; then
    FOUNDRY_PROJECT_NAME="${BASH_REMATCH[1]}"
    return 0
  fi
  echo "Could not parse project name from Foundry project endpoint: ${endpoint}" >&2
  exit 1
}

foundry_require_arm_account_name() {
  : "${AZURE_FOUNDRY_ARM_ACCOUNT_NAME:?AZURE_FOUNDRY_ARM_ACCOUNT_NAME is required (e.g. wx1116-prod-res)}"
  FOUNDRY_ARM_ACCOUNT_NAME="$AZURE_FOUNDRY_ARM_ACCOUNT_NAME"
}

foundry_arm_connection_url() {
  local subscription_id="$1"
  local resource_group="$2"
  local account_name="$3"
  local project_name="$4"
  local connection_name="$5"
  printf 'https://management.azure.com/subscriptions/%s/resourceGroups/%s/providers/Microsoft.CognitiveServices/accounts/%s/projects/%s/connections/%s?api-version=%s' \
    "$subscription_id" "$resource_group" "$account_name" "$project_name" "$connection_name" "$FOUNDRY_ARM_CONNECTION_API_VERSION"
}

foundry_upsert_arm_connection() {
  local subscription_id="$1"
  local resource_group="$2"
  local account_name="$3"
  local project_name="$4"
  local connection_name="$5"
  local properties_json="$6"
  local response_file="$7"

  local arm_token url request_body http_status
  arm_token="$(az account get-access-token --resource https://management.azure.com --query accessToken -o tsv)"
  url="$(foundry_arm_connection_url "$subscription_id" "$resource_group" "$account_name" "$project_name" "$connection_name")"
  request_body="$(jq -n --argjson properties "$properties_json" '{properties: $properties}')"

  http_status=$(curl -sS -o "$response_file" -w '%{http_code}' \
    -X PUT "$url" \
    -H "Authorization: Bearer ${arm_token}" \
    -H "Content-Type: application/json" \
    -d "$request_body")

  printf '%s' "$http_status"
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

  local app_json func_json python_json node_json
  app_json="$(foundry_fetch_connection "$project_endpoint" "$FOUNDRY_MCP_APP_CONNECTION_NAME" "${FOUNDRY_MCP_APP_CONNECTION_JSON:-}")"
  func_json="$(foundry_fetch_connection "$project_endpoint" "$FOUNDRY_MCP_FUNC_CONNECTION_NAME" "${FOUNDRY_MCP_FUNC_CONNECTION_JSON:-}")"
  python_json="$(foundry_fetch_connection "$project_endpoint" "$FOUNDRY_MCP_PYTHON_CONNECTION_NAME" "${FOUNDRY_MCP_PYTHON_CONNECTION_JSON:-}")"
  node_json="$(foundry_fetch_connection "$project_endpoint" "$FOUNDRY_MCP_NODE_CONNECTION_NAME" "${FOUNDRY_MCP_NODE_CONNECTION_JSON:-}")"

  FOUNDRY_MCP_APP_CONNECTION_ID="$(foundry_connection_id <<<"$app_json")"
  FOUNDRY_MCP_FUNC_CONNECTION_ID="$(foundry_connection_id <<<"$func_json")"
  FOUNDRY_MCP_PYTHON_CONNECTION_ID="$(foundry_connection_id <<<"$python_json")"
  FOUNDRY_MCP_NODE_CONNECTION_ID="$(foundry_connection_id <<<"$node_json")"
  FOUNDRY_MCP_APP_TARGET="$(foundry_connection_target <<<"$app_json")"
  FOUNDRY_MCP_FUNC_TARGET="$(foundry_connection_target <<<"$func_json")"
  FOUNDRY_MCP_PYTHON_TARGET="$(foundry_connection_target <<<"$python_json")"
  FOUNDRY_MCP_NODE_TARGET="$(foundry_connection_target <<<"$node_json")"

  if [ -z "$FOUNDRY_MCP_APP_CONNECTION_ID" ]; then
    FOUNDRY_MCP_APP_CONNECTION_ID="$FOUNDRY_MCP_APP_CONNECTION_NAME"
  fi
  if [ -z "$FOUNDRY_MCP_FUNC_CONNECTION_ID" ]; then
    FOUNDRY_MCP_FUNC_CONNECTION_ID="$FOUNDRY_MCP_FUNC_CONNECTION_NAME"
  fi
  if [ -z "$FOUNDRY_MCP_PYTHON_CONNECTION_ID" ]; then
    FOUNDRY_MCP_PYTHON_CONNECTION_ID="$FOUNDRY_MCP_PYTHON_CONNECTION_NAME"
  fi
  if [ -z "$FOUNDRY_MCP_NODE_CONNECTION_ID" ]; then
    FOUNDRY_MCP_NODE_CONNECTION_ID="$FOUNDRY_MCP_NODE_CONNECTION_NAME"
  fi

  if [ -z "$FOUNDRY_MCP_APP_TARGET" ]; then
    : "${MCP_SRV_APP_SERVICE_URL:?MCP app-service URL required when the Foundry connection has no target}"
    FOUNDRY_MCP_APP_TARGET="${MCP_SRV_APP_SERVICE_URL%/}/mcp"
  fi
  if [ -z "$FOUNDRY_MCP_FUNC_TARGET" ]; then
    : "${MCP_SRV_FUNC_APP_URL:?MCP func-app URL required when the Foundry connection has no target}"
    FOUNDRY_MCP_FUNC_TARGET="${MCP_SRV_FUNC_APP_URL%/}/runtime/webhooks/mcp"
  fi
  if [ -z "$FOUNDRY_MCP_PYTHON_TARGET" ]; then
    : "${MCP_SRV_PYTHON_URL:?MCP python URL required when the Foundry connection has no target}"
    FOUNDRY_MCP_PYTHON_TARGET="${MCP_SRV_PYTHON_URL%/}/mcp"
  fi
  if [ -z "$FOUNDRY_MCP_NODE_TARGET" ]; then
    : "${MCP_SRV_NODE_URL:?MCP node URL required when the Foundry connection has no target}"
    FOUNDRY_MCP_NODE_TARGET="${MCP_SRV_NODE_URL%/}/mcp"
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
    --arg pythonLabel "McpSrvPython" \
    --arg pythonUrl "$FOUNDRY_MCP_PYTHON_TARGET" \
    --arg pythonConn "$FOUNDRY_MCP_PYTHON_CONNECTION_ID" \
    --arg nodeLabel "McpSrvNode" \
    --arg nodeUrl "$FOUNDRY_MCP_NODE_TARGET" \
    --arg nodeConn "$FOUNDRY_MCP_NODE_CONNECTION_ID" \
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
      },
      {
        type: "mcp",
        server_label: $pythonLabel,
        server_url: $pythonUrl,
        project_connection_id: $pythonConn,
        require_approval: "never"
      },
      {
        type: "mcp",
        server_label: $nodeLabel,
        server_url: $nodeUrl,
        project_connection_id: $nodeConn,
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
