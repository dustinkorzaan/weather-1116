#!/bin/bash
set -euo pipefail

if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

cd "$CLAUDE_PROJECT_DIR"

apt-get update
apt-get install -y dotnet-sdk-10.0

export NVM_DIR="$HOME/.nvm"
. /opt/nvm/nvm.sh
nvm install 24
{
  echo "export NVM_DIR=\"$NVM_DIR\""
  echo "export PATH=\"$(dirname "$(nvm which 24)"):\$PATH\""
} >> "$CLAUDE_ENV_FILE"

# npm install -g npm@latest

dotnet restore Weather.sln
npm install --prefix ui-react

# --- Sanity check ---
echo "node:   $(node --version)"
echo "npm:    $(npm --version)"
echo "git:    $(git --version)"
echo "dotnet: $(dotnet --version)"
