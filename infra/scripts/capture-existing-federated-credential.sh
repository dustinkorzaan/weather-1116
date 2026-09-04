#!/usr/bin/env bash
# Decide whether infra/modules/managed-identity.bicep should create the GitHub
# OIDC federated credential on wx1116-prod-github-actions-mi.
#
# Azure allows only one federated credential per issuer+subject pair on a managed
# identity. Bootstrap often creates that credential by hand before the first
# azd provision; a second create with a different resource name hits:
#   Conflict: Issuer and subject combination already exists for this Managed Identity.
#
# This script lists credentials on the identity and sets CREATE_GITHUB_FEDERATED_
# CREDENTIAL in the azd environment (true = Bicep creates github-actions-prod,
# false = an existing credential already covers repo:...:environment:prod).
#
# Run from the azure.yaml preprovision hook (after az login) or by hand:
#   az login
#   bash infra/scripts/capture-existing-federated-credential.sh
#   azd provision
#
# Usage: capture-existing-federated-credential.sh [resource-group] [identity-name] [github-repo] [environment-name]

set -euo pipefail

RESOURCE_GROUP="${1:-${AZURE_RESOURCE_GROUP:-wx1116-prod-rg}}"
IDENTITY_NAME="${2:-${GITHUB_ACTIONS_IDENTITY_NAME:-wx1116-prod-github-actions-mi}}"
GITHUB_REPO="${3:-${GITHUB_REPOSITORY:-dustinkorzaan/weather-1116}}"
ENVIRONMENT_NAME="${4:-${AZURE_ENV_NAME:-prod}}"

ISSUER='https://token.actions.githubusercontent.com'
SUBJECT="repo:${GITHUB_REPO}:environment:${ENVIRONMENT_NAME}"

if ! az identity show \
  --name "$IDENTITY_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --output none 2>/dev/null; then
  cat >&2 <<EOF
GitHub Actions managed identity $IDENTITY_NAME was not found in $RESOURCE_GROUP.
Create it manually before provisioning (see docs/aca-bootstrap.md).
EOF
  exit 1
fi

CREDS_JSON="$(az identity federated-credential list \
  --identity-name "$IDENTITY_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --output json)"

MATCHING_NAME="$(jq -r --arg issuer "$ISSUER" --arg subject "$SUBJECT" \
  '.[] | select(.issuer == $issuer and .subject == $subject) | .name' \
  <<<"$CREDS_JSON" | head -1)"

if [ -n "$MATCHING_NAME" ]; then
  echo "Federated credential already covers $SUBJECT (name: $MATCHING_NAME); skipping Bicep creation."
  CREATE=false
else
  echo "No federated credential for $SUBJECT; Bicep will create github-actions-${ENVIRONMENT_NAME}."
  CREATE=true
fi

azd env set CREATE_GITHUB_FEDERATED_CREDENTIAL "$CREATE"
