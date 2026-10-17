#!/usr/bin/env bash
# Offline readiness and system-key checks. Does not call Azure.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
SCRIPT="${ROOT}/.github/scripts/aca-functions-mcp-key.sh"

fail() {
  echo "FAIL: $*" >&2
  exit 1
}

az() {
  case "$*" in
    "containerapp function keys list --help") return 0 ;;
    "containerapp show "*)
      echo "test-revision" ;;
    "containerapp revision show "*)
      jq -n --arg running "$TEST_RUNNING" --arg health "$TEST_HEALTH" \
        '{runningState: $running, healthState: $health}' ;;
    "containerapp function keys list "*)
      echo "${TEST_CURRENT_KEY:-}" ;;
    "containerapp function keys set "*)
      [[ "$*" == *"--key-type systemKey --key-name mcp_extension --key-value offline-test-key --output none" ]] \
        || return 1
      echo "MOCK: system key set" ;;
    *) echo "Unexpected az invocation: $*" >&2; return 1 ;;
  esac
}

# Advance the existing timeout without actually waiting.
sleep() { :; }
export -f az sleep
export MCP_EXTENSION_KEY="offline-test-key"
export TEST_RUNNING TEST_HEALTH TEST_CURRENT_KEY

for TEST_RUNNING in Running RunningAtMaxScale; do
  TEST_HEALTH=Healthy
  TEST_CURRENT_KEY=""
  OUTPUT="$(bash "$SCRIPT" test-app test-rg)"
  [[ "$OUTPUT" == *"MOCK: system key set"* ]] \
    || fail "$TEST_RUNNING/Healthy should set the system key"

  TEST_CURRENT_KEY="$MCP_EXTENSION_KEY"
  OUTPUT="$(bash "$SCRIPT" test-app test-rg)"
  [[ "$OUTPUT" == *"already matches"* && "$OUTPUT" != *"MOCK: system key set"* ]] \
    || fail "$TEST_RUNNING/Healthy should leave a matching key unchanged"
done

TEST_CURRENT_KEY=""
for TEST_RUNNING in Running RunningAtMaxScale Activating Failed Degraded Stopped; do
  TEST_HEALTH=Unhealthy
  if OUTPUT="$(bash "$SCRIPT" test-app test-rg 2>&1)"; then
    fail "$TEST_RUNNING/Unhealthy should not be accepted"
  fi
  [[ "$OUTPUT" != *"MOCK: system key set"* ]] \
    || fail "$TEST_RUNNING/Unhealthy must not set keys"
  case "$TEST_RUNNING" in
    Failed|Degraded|Stopped)
      [[ "$OUTPUT" == *"terminal state"* ]] || fail "expected terminal failure" ;;
    *)
      [[ "$OUTPUT" == *"within 300s"* ]] || fail "expected readiness timeout" ;;
  esac
done

echo "OK: Functions system keys require a healthy running revision, including RunningAtMaxScale"
