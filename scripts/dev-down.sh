#!/usr/bin/env bash
# Stops the services started by dev-up.sh. Pass --all to also stop the Docker containers (data is kept).
set -uo pipefail
cd "$(dirname "$0")/.."

pkill -f "MiniKobo.Catalog.Api" && echo "Stopped Catalog API"
pkill -f "MiniKobo.Orders.Api" && echo "Stopped Orders API"
pkill -f "MiniKobo.Orders.Worker" && echo "Stopped Orders.Worker"
pkill -f "next dev" && echo "Stopped web app"

if [ "${1:-}" = "--all" ]; then
  docker compose stop
fi
