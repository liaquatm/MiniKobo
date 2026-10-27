# MiniKobo

[![CI](https://github.com/liaquatm/MiniKobo/actions/workflows/ci.yml/badge.svg)](https://github.com/liaquatm/MiniKobo/actions/workflows/ci.yml)

A small, working online bookstore built to practise a modern .NET e-commerce stack end to end: browse and search books, manage a cart, check out, and watch the order move through **asynchronous, message-driven processing** (stock reservation, simulated payment, confirmation email) in real time.

The focus is on **clean code and sound architecture** rather than feature count. Every significant decision, and why it was made, is recorded in [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Tech stack

| Area | Technology | Used for |
|---|---|---|
| Back end | **C#, ASP.NET Core Web API (.NET 10)** | Catalog API (Minimal APIs) and Orders API (MVC controllers) |
| NoSQL | **RavenDB** | Book catalog, full-text search index, stock with optimistic concurrency |
| Relational | **PostgreSQL + EF Core** | Carts and orders (migrations), NServiceBus outbox and saga state |
| Messaging | **NServiceBus on RabbitMQ** | Commands, events, an order-processing **saga**, the **outbox** pattern, retries |
| Front end | **Next.js 16, React 19, TypeScript, Tailwind CSS** | Storefront: catalog, search, cart, checkout, live order status |
| Resilience | **Microsoft.Extensions.Http.Resilience (Polly)** | Retries, timeouts and a circuit breaker on service-to-service HTTP calls |
| Testing | **NUnit, Testcontainers, NServiceBus.Testing, Playwright** | Unit, integration (real databases in Docker), saga and end-to-end UI tests |
| Infrastructure | **Docker Compose** | RavenDB, PostgreSQL, RabbitMQ and Mailpit (a fake SMTP inbox) locally |
| CI | **GitHub Actions** | Build, all test suites, and Playwright against the full stack on every push |

## Architecture

```
            ┌───────────────────────────────┐
 Browser ──►│ Next.js storefront (port 3000)│  /api/catalog/* and /api/orders/* are proxied
            └──────┬─────────────────┬──────┘  to the APIs (one origin, no CORS)
                   │                 │
                   ▼                 ▼
        ┌─────────────────┐   HTTP (price, stock)   ┌──────────────────┐
        │  Catalog API    │◄────────────────────────│   Orders API     │
        │  (5100)         │   resilient HttpClient  │   (5200)         │
        └───┬─────────▲───┘                         └───┬─────────▲────┘
            │         │                                 │         │ PlaceOrder -> save order
         RavenDB      │                             PostgreSQL    │ + publish OrderPlaced
                      │                                 ▲         │ (outbox, one transaction)
        ReserveStock /│ReleaseStock                     │         │
        StockReserved │(replies)          saga state ───┘         │ OrderStatusChanged
                      │                                           │
                 ┌────┴───────────────────────────────────────────┴───┐
                 │     Orders.Worker: OrderProcessingSaga             │
                 │  reserve stock → payment → email → confirmed       │
                 │  (payment fails → release stock = compensation)    │
                 └────────────────────────┬───────────────────────────┘
                                          │ SMTP
                                       Mailpit
             All messages travel over RabbitMQ via NServiceBus.
```

**Checkout flow**
1. `POST /orders` validates the cart and sends a `PlaceOrder` command, returning **202 Accepted** immediately.
2. Its handler saves the order **and** publishes `OrderPlaced` in one database transaction (the **outbox**), so an order can never be saved without its event, or the other way round.
3. The **saga** in Orders.Worker reserves stock in Catalog, then takes a simulated payment, then sends the confirmation email. It publishes `OrderStatusChanged` after each step.
4. If payment fails, the saga **compensates** by releasing the reserved stock. Stock handlers are **idempotent**, and RavenDB **optimistic concurrency** stops two orders taking the last copy.
5. The storefront polls the order and shows each step as it completes.

## Run it

**Prerequisites:** .NET 10 SDK, Node.js 20+, Docker Desktop.

First time only:
```bash
cp .env.example .env            # then set POSTGRES_PASSWORD in .env
dotnet tool restore             # installs dotnet-ef (pinned in dotnet-tools.json)
dotnet user-secrets set "ConnectionStrings:Orders" "Host=localhost;Port=5432;Database=minikobo_orders;Username=minikobo;Password=<your .env password>" --project src/MiniKobo.Orders.Api
```

Then start everything (infrastructure, migrations, three .NET services, web app):
```bash
scripts/dev-up.sh
```
Open **http://localhost:3000**. Stop with `scripts/dev-down.sh` (add `--all` to also stop the containers).

| Tool | URL |
|---|---|
| Storefront | http://localhost:3000 |
| Mailpit (confirmation emails) | http://localhost:8025 |
| RabbitMQ management (`guest`/`guest`) | http://localhost:15672 |
| RavenDB Studio | http://localhost:8080 |

### Things to try
- **Search** for `dickens`, `whale` (matches descriptions) or `jane`.
- **Check out**, and watch the order go *placed → stock reserved → paid → confirmed*, then find the email in Mailpit.
- **Declined payment:** use an email containing `+fail` (e.g. `you+fail@example.com`). The order fails and the stock is put back.
- **Last copy:** *The Count of Monte Cristo* starts with 1 copy. If two customers check it out at once, exactly one succeeds.
- **Out of stock:** *Crime and Punishment* can't be added to a cart.

## Tests

```bash
dotnet test                       # unit + integration (Docker must be running)
npm --prefix web run test:e2e     # Playwright, against the running stack
```

| Suite | What it covers |
|---|---|
| `*.UnitTests` | Cart and order rules, paging validation, seed data, and the saga's decisions (`NServiceBus.Testing`) |
| `*.IntegrationTests` | Each API in memory (`WebApplicationFactory`) against a real RavenDB or PostgreSQL started by **Testcontainers** |
| `web/e2e` | A real browser buying a book end to end, plus the declined-payment path |

## Project structure

```
src/
  MiniKobo.Catalog.Api      Books, search, stock (RavenDB); handles stock commands
  MiniKobo.Orders.Api       Carts and orders (EF Core, PostgreSQL); publishes OrderPlaced
  MiniKobo.Orders.Worker    Order-processing saga, simulated payment, email
  MiniKobo.Contracts        Message types shared between services
tests/                      NUnit unit and integration tests per service
web/                        Next.js storefront and Playwright tests
scripts/                    dev-up.sh / dev-down.sh
docs/                       ARCHITECTURE.md (decisions), ROADMAP.md, LEARNING-NOTES.md
```

## What's next

Planned in [docs/ROADMAP.md](docs/ROADMAP.md): customer accounts with ASP.NET Core Identity, real-time status with SignalR instead of polling, and containerising the services themselves.
