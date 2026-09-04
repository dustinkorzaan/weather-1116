#!/usr/bin/env bash
# Tell azd (and through it infra/main.bicep) which wx1116-prod-* container apps
# already exist, so that `azd provision` preserves the image, environment
# variables, and secrets that .github/workflows/prod-deploy-*.yml owns instead
# of resetting them to the placeholder image and a bare app-settings list.
#
# A Bicep deployment is a PUT, and infra/modules/existing-container-app.bicep can
# only read values back off an app that is actually there, so existence has to be
# resolved before provision runs. Hence this pre-pass:
#
#   az login
#   bash infra/scripts/capture-existing-container-apps.sh
#   azd provision
#
# prod-provision-infra.yml runs it on every provision. Run it by hand the same
# way before provisioning locally.
#
# Usage: capture-existing-container-apps.sh [resource-group] [name-prefix] [environment-name]

set -euo pipefail

RESOURCE_GROUP="${1:-${AZURE_RESOURCE_GROUP:-wx1116-prod-rg}}"
NAME_PREFIX="${2:-${AZURE_NAME_PREFIX:-wx1116}}"
ENVIRONMENT_NAME="${3:-${AZURE_ENV_NAME:-prod}}"

# Must match containerAppsConfig plus the Functions host in infra/main.bicep.
APP_KEYS=(api mvc blazor worker mcp-srv-app-service mcp-srv-func-app)

# `az resource list` rather than `az containerapp list`: it needs no CLI
# extension and returns an empty list (instead of an error) when the
# Microsoft.App provider has not been registered yet, which is the state on a
# genuinely greenfield subscription.
if ! LIVE_NAMES="$(az resource list \
  --resource-group "$RESOURCE_GROUP" \
  --resource-type Microsoft.App/containerApps \
  --query "[].name" \
  --output tsv)"; then
  cat >&2 <<EOF
Could not list container apps in $RESOURCE_GROUP.
Refusing to continue: reporting the apps as absent would make azd provision
reset every live image, environment variable, and secret to the Bicep
placeholder. Check that az is logged in and the resource group name is correct.
EOF
  exit 1
fi

EXISTING=""
for key in "${APP_KEYS[@]}"; do
  if grep -qxF "${NAME_PREFIX}-${ENVIRONMENT_NAME}-${key}" <<<"$LIVE_NAMES"; then
    EXISTING="${EXISTING:+${EXISTING},}${key}"
  fi
done

if [ -z "$EXISTING" ]; then
  echo "No container apps in $RESOURCE_GROUP yet; provision will create all of them from the placeholder image."
else
  echo "Preserving deploy-owned image, env, and secrets for: $EXISTING"
fi

azd env set EXISTING_CONTAINER_APP_KEYS "$EXISTING"
