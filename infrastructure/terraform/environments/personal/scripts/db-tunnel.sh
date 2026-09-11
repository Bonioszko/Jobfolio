#!/usr/bin/env bash
set -Eeuo pipefail

readonly JOBPARSER_PROJECT_ID="${JOBPARSER_GCP_PROJECT_ID:?Set JOBPARSER_GCP_PROJECT_ID to the target Google Cloud project ID.}"
readonly JOBPARSER_ZONE="${JOBPARSER_GCP_ZONE:-us-central1-a}"
readonly JOBPARSER_VM_NAME="${JOBPARSER_DB_VM_NAME:-jobparser-db}"
readonly JOBPARSER_LOCAL_PORT="${JOBPARSER_DB_LOCAL_PORT:-5433}"

set -- gcloud compute ssh "$JOBPARSER_VM_NAME" \
  --project="$JOBPARSER_PROJECT_ID" \
  --zone="$JOBPARSER_ZONE" \
  --tunnel-through-iap

if [[ -n "${JOBPARSER_SSH_KEY_FILE:-}" ]]; then
  set -- "$@" "--ssh-key-file=${JOBPARSER_SSH_KEY_FILE}"
fi

echo "Opening Job Parser database tunnel on 127.0.0.1:${JOBPARSER_LOCAL_PORT}"
echo "Keep this process running and press Ctrl-C to close the tunnel."

exec "$@" \
  -- \
  -N \
  -o ExitOnForwardFailure=yes \
  -o ServerAliveInterval=30 \
  -L "127.0.0.1:${JOBPARSER_LOCAL_PORT}:127.0.0.1:5432"
