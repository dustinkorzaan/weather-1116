#!/usr/bin/env bash
# Resolve secretref values against a secrets file and upsert the merged
# NAME=VALUE pairs as Azure App Service / Function App application settings.
#
# Unlike Container Apps, `az webapp/functionapp config appsettings set` only
# touches the keys it's given -- it doesn't replace the whole settings list --
# so no read-back-and-merge step is needed here.
#
# Usage:
#   app-service-configure.sh --app-name NAME --resource-group RG [--app-kind webapp|functionapp] \
#     [--secrets-file FILE] [--env-overlay-file FILE]
#
# secrets-file: lines of secret-name=secret-value (optional)
# env-overlay-file: lines of NAME=value or NAME=secretref:secret-name (optional)

set -euo pipefail

APP_NAME=""
RESOURCE_GROUP=""
APP_KIND="webapp"
SECRETS_FILE=""
ENV_OVERLAY_FILE=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --app-name)
      APP_NAME="${2:?}"
      shift 2
      ;;
    --resource-group)
      RESOURCE_GROUP="${2:?}"
      shift 2
      ;;
    --app-kind)
      APP_KIND="${2:?}"
      shift 2
      ;;
    --secrets-file)
      SECRETS_FILE="${2:?}"
      shift 2
      ;;
    --env-overlay-file)
      ENV_OVERLAY_FILE="${2:?}"
      shift 2
      ;;
    *)
      echo "Unknown argument: $1" >&2
      exit 1
      ;;
  esac
done

if [ -z "$APP_NAME" ] || [ -z "$RESOURCE_GROUP" ]; then
  echo "app name and resource group are required." >&2
  exit 1
fi

declare -A SECRET_VALUES
if [ -n "$SECRETS_FILE" ] && [ -f "$SECRETS_FILE" ]; then
  while IFS= read -r line || [ -n "$line" ]; do
    [[ -z "$line" || "$line" =~ ^# ]] && continue
    SECRET_VALUES["${line%%=*}"]="${line#*=}"
  done < "$SECRETS_FILE"
fi

if [ -z "$ENV_OVERLAY_FILE" ] || [ ! -f "$ENV_OVERLAY_FILE" ]; then
  echo "No env overlay specified; nothing to update on $APP_NAME."
  exit 0
fi

SETTINGS_ARGS=()
while IFS= read -r line || [ -n "$line" ]; do
  [[ -z "$line" || "$line" =~ ^# ]] && continue

  KEY="${line%%=*}"
  VALUE="${line#*=}"

  if [[ "$VALUE" == secretref:* ]]; then
    SECRET_NAME="${VALUE#secretref:}"
    VALUE="${SECRET_VALUES[$SECRET_NAME]:-}"
    if [ -z "$VALUE" ]; then
      echo "secretref:$SECRET_NAME has no matching entry in the secrets file." >&2
      exit 1
    fi
  fi

  SETTINGS_ARGS+=("${KEY}=${VALUE}")
done < "$ENV_OVERLAY_FILE"

if [ "${#SETTINGS_ARGS[@]}" -eq 0 ]; then
  echo "Merged settings list is empty; refusing to update $APP_NAME." >&2
  exit 1
fi

if [ "$APP_KIND" = "functionapp" ]; then
  az functionapp config appsettings set \
    --name "$APP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --settings "${SETTINGS_ARGS[@]}" \
    --output none
else
  az webapp config appsettings set \
    --name "$APP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --settings "${SETTINGS_ARGS[@]}" \
    --output none
fi

echo "Updated $APP_NAME application settings."
