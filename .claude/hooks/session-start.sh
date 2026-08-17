#!/bin/bash
set -euo pipefail

if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

DOTNET_CHANNEL="10.0"

# The dot.net CDN (builds.dotnet.microsoft.com) is blocked by this environment's
# egress policy, so install via apt using Microsoft's Ubuntu package feed instead.
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

dotnet restore core-dotnet/core.tests/Core.Tests.csproj
