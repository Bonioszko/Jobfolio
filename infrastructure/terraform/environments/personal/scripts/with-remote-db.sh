#!/usr/bin/env bash
set -Eeuo pipefail

readonly JOBPARSER_PROJECT_ID="${JOBPARSER_GCP_PROJECT_ID:?Set JOBPARSER_GCP_PROJECT_ID to the target Google Cloud project ID.}"
readonly JOBPARSER_SECRET_ID="${JOBPARSER_DB_SECRET_ID:-jobparser-postgres-password}"
readonly JOBPARSER_LOCAL_PORT="${JOBPARSER_DB_LOCAL_PORT:-5433}"

if [[ "$#" -eq 0 ]]; then
  echo "Usage: $0 <command> [arguments...]" >&2
  exit 64
fi

if ! nc -z 127.0.0.1 "$JOBPARSER_LOCAL_PORT" >/dev/null 2>&1; then
  echo "No database tunnel is listening on 127.0.0.1:${JOBPARSER_LOCAL_PORT}." >&2
  echo "Start scripts/db-tunnel.sh in another terminal first." >&2
  exit 1
fi

if ! jobparser_db_password="$(gcloud secrets versions access latest \
  --project="$JOBPARSER_PROJECT_ID" \
  --secret="$JOBPARSER_SECRET_ID")"; then
  echo "Could not read the PostgreSQL password from Secret Manager." >&2
  exit 1
fi

if [[ -z "$jobparser_db_password" ]]; then
  echo "Secret Manager returned an empty PostgreSQL password." >&2
  exit 1
fi

export ConnectionStrings__Postgres="Host=127.0.0.1;Port=${JOBPARSER_LOCAL_PORT};Database=jobparser;Username=jobparser;Password=${jobparser_db_password};SSL Mode=Disable;Maximum Pool Size=10"
unset jobparser_db_password

exec "$@"
