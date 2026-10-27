#!/usr/bin/env bash
# Starts the whole MiniKobo stack locally: Docker infrastructure, the three .NET services and the web app.
# Logs go to .logs/. Stop everything with scripts/dev-down.sh.
set -euo pipefail
cd "$(dirname "$0")/.."
mkdir -p .logs

start() {
  local name=$1
  shift
  echo "Starting $name (log: .logs/$name.log)"
  nohup "$@" > ".logs/$name.log" 2>&1 &
}

wait_http() {
  curl -s -o /dev/null --retry 120 --retry-connrefused --retry-delay 1 "$1"
}

echo "Starting infrastructure: RavenDB, PostgreSQL, RabbitMQ, Mailpit"
docker compose up -d
until docker compose exec -T postgres pg_isready -U minikobo -q; do sleep 1; done
until docker compose exec -T rabbitmq rabbitmq-diagnostics -q check_running > /dev/null 2>&1; do sleep 2; done

echo "Building and migrating"
dotnet tool restore > /dev/null
dotnet build --nologo -v quiet
dotnet ef database update --project src/MiniKobo.Orders.Api --no-build

start catalog dotnet run --no-build --project src/MiniKobo.Catalog.Api
start orders dotnet run --no-build --project src/MiniKobo.Orders.Api
wait_http http://localhost:5100/books
wait_http http://localhost:5200/orders/00000000-0000-0000-0000-000000000000

# Started after the Orders API: on first run both create NServiceBus tables in the same database.
start worker dotnet run --no-build --project src/MiniKobo.Orders.Worker

[ -d web/node_modules ] || npm --prefix web ci
start web npm --prefix web run dev
wait_http http://localhost:3000

cat <<EOF

MiniKobo is running
  Store:          http://localhost:3000
  Catalog API:    http://localhost:5100/books
  Orders API:     http://localhost:5200
  RavenDB Studio: http://localhost:8080
  RabbitMQ:       http://localhost:15672  (guest / guest)
  Mailpit inbox:  http://localhost:8025
EOF
