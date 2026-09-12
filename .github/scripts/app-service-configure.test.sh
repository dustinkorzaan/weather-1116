#!/usr/bin/env bash
# Offline checks for app-service-configure.sh. Stubs `az` on PATH so no live
# Azure calls happen; asserts on the arguments the stub recorded.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
SCRIPT="${ROOT}/.github/scripts/app-service-configure.sh"

fail() {
  echo "FAIL: $*" >&2
  exit 1
}

STUB_DIR="$(mktemp -d)"
trap 'rm -rf "$STUB_DIR"' EXIT

export CALL_LOG="${STUB_DIR}/az-calls.log"

cat > "${STUB_DIR}/az" <<'STUB'
#!/usr/bin/env bash
echo "$@" >> "$CALL_LOG"
exit 0
STUB
chmod +x "${STUB_DIR}/az"

run_script() {
  : > "$CALL_LOG"
  PATH="${STUB_DIR}:${PATH}" bash "$SCRIPT" "$@"
}

SECRETS_FILE="${STUB_DIR}/secrets.env"
ENV_FILE="${STUB_DIR}/env.env"
cat > "$SECRETS_FILE" <<'EOF'
db-connection-string=super-secret-value
EOF
cat > "$ENV_FILE" <<'EOF'
DB_CONNECTION_STRING=secretref:db-connection-string
ASPNETCORE_DETAILEDERRORS=false
EOF

# 1. secretref resolution: the secret value is substituted into the az call;
# the raw "secretref:name" form must never reach az.
run_script --app-name wx1116-prod-api --resource-group wx1116-prod-rg --app-kind webapp \
  --secrets-file "$SECRETS_FILE" --env-overlay-file "$ENV_FILE" >/dev/null

CALL="$(cat "$CALL_LOG")"
[[ "$CALL" == "webapp config appsettings set"* ]] || fail "expected a webapp appsettings set call, got: $CALL"
[[ "$CALL" == *"DB_CONNECTION_STRING=super-secret-value"* ]] || fail "secretref was not resolved: $CALL"
[[ "$CALL" != *"secretref:"* ]] || fail "raw secretref leaked into the az call: $CALL"
[[ "$CALL" == *"ASPNETCORE_DETAILEDERRORS=false"* ]] || fail "plain value missing: $CALL"
[[ "$CALL" == *"--name wx1116-prod-api"* ]] || fail "app name missing: $CALL"

# 2. app-kind functionapp routes to `az functionapp config appsettings set`.
run_script --app-name wx1116-prod-mcp-srv-func-app --resource-group wx1116-prod-rg --app-kind functionapp \
  --secrets-file "$SECRETS_FILE" --env-overlay-file "$ENV_FILE" >/dev/null
CALL="$(cat "$CALL_LOG")"
[[ "$CALL" == "functionapp config appsettings set"* ]] || fail "functionapp kind should call functionapp config, got: $CALL"

# 3. empty overlay: nothing to update, az must not be called at all.
run_script --app-name wx1116-prod-blazor --resource-group wx1116-prod-rg --app-kind webapp >/dev/null
[[ ! -s "$CALL_LOG" ]] || fail "az should not be called with no env overlay, got: $(cat "$CALL_LOG")"

# 4. missing secret: a secretref referencing an absent secret must fail
# loudly, not silently pass through or fall back to an empty value.
BAD_ENV_FILE="${STUB_DIR}/bad-env.env"
cat > "$BAD_ENV_FILE" <<'EOF'
DB_CONNECTION_STRING=secretref:does-not-exist
EOF
if run_script --app-name wx1116-prod-api --resource-group wx1116-prod-rg --app-kind webapp \
  --secrets-file "$SECRETS_FILE" --env-overlay-file "$BAD_ENV_FILE" >/dev/null 2>&1
then
  fail "missing secretref target should fail, but the script exited 0"
fi
[[ ! -s "$CALL_LOG" ]] || fail "az should not be called when a secretref is unresolved, got: $(cat "$CALL_LOG")"

echo "OK: app-service-configure.sh resolves secretref, empty overlay is a no-op, missing secretref fails loudly"
