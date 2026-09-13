#!/usr/bin/env bash

set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repository_root"

if [[ ! -f .env ]]; then
  cp .env.example .env
fi

set -a
source .env
set +a

docker compose up -d --wait postgres
dotnet restore JobParser.slnx --configfile NuGet.Config
npm --prefix frontend ci
dotnet run --project src/App.DatabaseMigrator --no-restore

dotnet run --project src/App.Api --launch-profile http &
api_pid=$!

dotnet run --project src/App.AiWorker &
ai_worker_pid=$!

dotnet run --project src/App.CompilerWorker &
compiler_pid=$!

npm --prefix frontend run dev &
frontend_pid=$!

cleanup() {
  trap - EXIT INT TERM
  kill "$api_pid" "$ai_worker_pid" "$compiler_pid" "$frontend_pid" 2>/dev/null || true
  wait "$api_pid" "$ai_worker_pid" "$compiler_pid" "$frontend_pid" 2>/dev/null || true
}

trap cleanup EXIT INT TERM

echo "Local app starting at http://localhost:5173"
echo "Press Ctrl+C to stop the API, AI worker, compiler worker, and frontend."

process_names=("API" "AI worker" "compiler worker" "frontend")
process_ids=("$api_pid" "$ai_worker_pid" "$compiler_pid" "$frontend_pid")

while true; do
  for index in "${!process_ids[@]}"; do
    process_id="${process_ids[$index]}"
    if ! kill -0 "$process_id" 2>/dev/null; then
      process_status=0
      wait "$process_id" || process_status=$?
      echo "${process_names[$index]} stopped unexpectedly (exit $process_status)."
      if [[ "$process_status" -eq 0 ]]; then
        process_status=1
      fi
      exit "$process_status"
    fi
  done

  sleep 1
done
