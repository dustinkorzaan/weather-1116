#!/bin/bash
set -euo pipefail

if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

cd "$CLAUDE_PROJECT_DIR"

# --- .NET 10 SDK ---------------------------------------------------------
# The dot.net CDN (builds.dotnet.microsoft.com) is blocked by this
# environment's egress policy, so install via apt using Microsoft's
# Ubuntu package feed instead.
DOTNET_CHANNEL="10.0"
if ! command -v dotnet >/dev/null 2>&1 || ! dotnet --list-sdks 2>/dev/null | grep -q "^${DOTNET_CHANNEL}\."; then
  if [ ! -f /etc/apt/sources.list.d/microsoft-prod.list ]; then
    . /etc/os-release
    curl -sSL -o /tmp/packages-microsoft-prod.deb \
      "https://packages.microsoft.com/config/ubuntu/${VERSION_ID}/packages-microsoft-prod.deb"
    dpkg -i /tmp/packages-microsoft-prod.deb
    rm -f /tmp/packages-microsoft-prod.deb
  fi
  apt-get update
  apt-get install -y "dotnet-sdk-${DOTNET_CHANNEL}"
fi

{
  echo "export DOTNET_MULTILEVEL_LOOKUP=0"
  echo "export DOTNET_NOLOGO=1"
  echo "export DOTNET_CLI_TELEMETRY_OPTOUT=1"
} >> "$CLAUDE_ENV_FILE"

# --- Node 24 ---------------------------------------------------------
# ui-react/package.json pins engines.node >=24 / npm >=11; the base image
# ships Node 22, so install 24 via the preinstalled nvm.
NODE_CHANNEL="24"
export NVM_DIR="${NVM_DIR:-$HOME/.nvm}"
# shellcheck source=/dev/null
. /opt/nvm/nvm.sh

if ! nvm ls "$NODE_CHANNEL" >/dev/null 2>&1; then
  nvm install "$NODE_CHANNEL"
fi
nvm use "$NODE_CHANNEL" >/dev/null

NODE_BIN_DIR="$(dirname "$(nvm which "$NODE_CHANNEL")")"
{
  echo "export NVM_DIR=\"$NVM_DIR\""
  echo "export PATH=\"$NODE_BIN_DIR:\$PATH\""
} >> "$CLAUDE_ENV_FILE"

# --- Restore / install dependencies ---------------------------------------
dotnet restore Weather.sln
npm install --prefix ui-react

# --- Sanity check ---------------------------------------------------------
echo "node:   $(node --version)"
echo "npm:    $(npm --version)"
echo "git:    $(git --version)"
echo "dotnet: $(dotnet --version)"
