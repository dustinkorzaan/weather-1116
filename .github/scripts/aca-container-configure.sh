#!/usr/bin/env bash
# Merge Container App environment variables instead of replacing the full list.
# az containerapp update --set-env-vars replaces all vars; this script overlays
# deploy-time values onto the vars Bicep provisioned (App Insights, UAMI, etc.).
#
# Usage:
#   aca-container-configure.sh --container-app NAME --resource-group RG \
#     [--secrets-file FILE] [--env-overlay-file FILE]
#
# secrets-file: lines of secret-name=secret-value (optional)
# env-overlay-file: lines of ENV_NAME=value or ENV_NAME=secretref:secret-name (optional)

set -euo pipefail

APP_NAME=""
RESOURCE_GROUP=""
SECRETS_FILE=""
ENV_OVERLAY_FILE=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --container-app)
      APP_NAME="${2:?}"
      shift 2
      ;;
    --resource-group)
      RESOURCE_GROUP="${2:?}"
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
      if [ -z "$APP_NAME" ]; then
        APP_NAME="$1"
      elif [ -z "$RESOURCE_GROUP" ]; then
        RESOURCE_GROUP="$1"
      elif [ -z "$SECRETS_FILE" ]; then
        SECRETS_FILE="$1"
      elif [ -z "$ENV_OVERLAY_FILE" ]; then
        ENV_OVERLAY_FILE="$1"
      else
        echo "Unknown argument: $1" >&2
        exit 1
      fi
      shift
      ;;
  esac
done

if [ -z "$APP_NAME" ] || [ -z "$RESOURCE_GROUP" ]; then
  echo "container app name and resource group are required." >&2
  exit 1
fi

if [ -n "$SECRETS_FILE" ] && [ -f "$SECRETS_FILE" ]; then
  SECRET_ARGS=()
  while IFS= read -r line || [ -n "$line" ]; do
    [[ -z "$line" || "$line" =~ ^# ]] && continue
    SECRET_ARGS+=("$line")
  done < "$SECRETS_FILE"

  if [ "${#SECRET_ARGS[@]}" -gt 0 ]; then
    az containerapp secret set \
      --name "$APP_NAME" \
      --resource-group "$RESOURCE_GROUP" \
      --secrets "${SECRET_ARGS[@]}"
  fi
fi

if [ -z "$ENV_OVERLAY_FILE" ] || [ ! -f "$ENV_OVERLAY_FILE" ]; then
  echo "No env overlay file; skipping env merge."
  exit 0
fi

EXISTING_ENV="$(az containerapp show \
  --name "$APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query "properties.template.containers[0].env" \
  --output json)"

MERGED_ENV="$EXISTING_ENV"
while IFS= read -r line || [ -n "$line" ]; do
  [[ -z "$line" || "$line" =~ ^# ]] && continue

  KEY="${line%%=*}"
  VALUE="${line#*=}"

  if [[ "$VALUE" == secretref:* ]]; then
    SECRET_NAME="${VALUE#secretref:}"
    MERGED_ENV="$(jq --arg key "$KEY" --arg secret "$SECRET_NAME" \
      'map(select(.name != $key)) + [{"name": $key, "secretRef": $secret}]' \
      <<< "$MERGED_ENV")"
  else
    MERGED_ENV="$(jq --arg key "$KEY" --arg value "$VALUE" \
      'map(select(.name != $key)) + [{"name": $key, "value": $value}]' \
      <<< "$MERGED_ENV")"
  fi
done < "$ENV_OVERLAY_FILE"

ENV_ARGS=()
while IFS= read -r arg; do
  [ -n "$arg" ] && ENV_ARGS+=("$arg")
done < <(jq -r '.[] |
  if .secretRef then "\(.name)=secretref:\(.secretRef)"
  elif .value then "\(.name)=\(.value)"
  else empty end' <<< "$MERGED_ENV")

if [ "${#ENV_ARGS[@]}" -eq 0 ]; then
  echo "Merged env list is empty; refusing to update $APP_NAME." >&2
  exit 1
fi

az containerapp update \
  --name "$APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --set-env-vars "${ENV_ARGS[@]}"

echo "Merged ${#ENV_ARGS[@]} environment variables on $APP_NAME."
