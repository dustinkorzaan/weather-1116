#!/bin/bash
# Claude Code (cloud) session bootstrap. Idempotent: a resumed session with
# everything installed finishes in seconds. Every stack's deps are installed so
# scripts/verify.sh --all works immediately for every worker.
set -uo pipefail

if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

cd "$CLAUDE_PROJECT_DIR"

declare -a SUMMARY=()
step() {
  local name="$1"; shift
  if "$@"; then SUMMARY+=("ok    $name"); else SUMMARY+=("FAIL  $name"); fi
}

# --- .NET 10 SDK -------------------------------------------------------------
install_dotnet() {
  if dotnet --version 2>/dev/null | grep -q '^10\.'; then return 0; fi
  apt-get update -qq && apt-get install -y -qq dotnet-sdk-10.0
}
step "dotnet sdk 10" install_dotnet

# --- Node 24 via nvm ---------------------------------------------------------
export NVM_DIR="$HOME/.nvm"
install_node() {
  # shellcheck disable=SC1091
  . /opt/nvm/nvm.sh || return 1
  nvm install 24 >/dev/null || return 1
  nvm use 24 >/dev/null || return 1
  if [ -n "${CLAUDE_ENV_FILE:-}" ]; then
    {
      echo "export NVM_DIR=\"$NVM_DIR\""
      echo "export PATH=\"$(dirname "$(nvm which 24)"):\$PATH\""
    } >> "$CLAUDE_ENV_FILE"
  fi
}
step "node 24" install_node

# --- Python venv shared by mcp-srv-python and FoundryConsoleV*python ---------
VENV="$HOME/.venvs/weather"
install_python() {
  if [ ! -x "$VENV/bin/python" ]; then
    "$(command -v python3.12 || command -v python3)" -m venv "$VENV" || return 1
  fi
  if "$VENV/bin/python" -c "import pytest, pytest_asyncio, mcp, openai" 2>/dev/null; then return 0; fi
  "$VENV/bin/python" -m pip install -q \
    -e "./mcp-srv-python/mcp[dev]" \
    -e "./FoundryConsoleV2python[dev]" \
    -e "./FoundryConsoleV3python[dev]" \
    -e "./FoundryConsoleV4python[dev]"
}
step "python venv ($VENV)" install_python

# --- Bicep CLI (same binary CI's validate-bicep job downloads) ---------------
install_bicep() {
  if command -v bicep >/dev/null 2>&1; then return 0; fi
  curl -sSfL -o /usr/local/bin/bicep \
    https://github.com/Azure/bicep/releases/latest/download/bicep-linux-x64 \
    && chmod +x /usr/local/bin/bicep \
    && bicep --version >/dev/null
}
step "bicep cli" install_bicep

# --- Restore / install -------------------------------------------------------
step "dotnet restore" dotnet restore Weather.sln --nologo -v q
npm_ci() {
  # Skip when node_modules is already in sync with the lockfile.
  if [ "$1/node_modules/.package-lock.json" -nt "$1/package-lock.json" ]; then return 0; fi
  npm ci --prefix "$1" --no-audit --no-fund --loglevel=error >/dev/null
}
step "npm ci ui-react" npm_ci ui-react
step "npm ci mcp-srv-node" npm_ci mcp-srv-node

# verify.sh diffs against origin/main; keep that ref current.
step "git fetch origin main" git fetch -q origin main

# --- Summary -----------------------------------------------------------------
echo "session-start summary:"
printf '  %s\n' "${SUMMARY[@]}"
echo "  node $(node --version 2>/dev/null) | npm $(npm --version 2>/dev/null) | dotnet $(dotnet --version 2>/dev/null) | python $("$VENV/bin/python" --version 2>/dev/null)"
echo "Run scripts/verify.sh to check your changes; /ship <story> runs the full pipeline."
exit 0
