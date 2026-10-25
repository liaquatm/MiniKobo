# MiniKobo Architecture

This document records **what** we decided and **why**. When a decision changes, update it here and add a row to the [Decision log](#decision-log).

See [ROADMAP.md](ROADMAP.md) for the milestone plan.

---

## 1. Overview

MiniKobo is a small online bookstore. Customers browse and search books, manage a cart, check out as a guest, and follow their order's status while it is processed asynchronously.

```
 ┌──────────────────┐        HTTP/JSON
 │ Next.js frontend │ ─────────────────────────────┐
 └──────────────────┘                              │
          │ browse/search                          │ cart, checkout, order status
          ▼                                        ▼
 ┌──────────────────┐   HTTP (price lookup)  ┌───────────────────┐
 │  Catalog API     │ ◄───────────────────── │  Orders API       │
 │  (ASP.NET Core)  │                        │  (ASP.NET Core)   │
 └───────┬──────────┘                        └────────┬──────────┘
         │                                            │ writes Order + publishes event
         ▼                                            ▼ (atomically, via outbox)
     RavenDB                                      PostgreSQL
         ▲                                            ▲
         │ reserve/release stock                      │ update status
 ┌───────┴──────────┐     RabbitMQ (NServiceBus)    ┌─┴─────────────────────┐
 │ Catalog handlers │ ◄──────────────────────────── │ Order processing saga │
 │ (in Catalog API) │ ─────────────────────────────►│ (Orders.Worker)       │
 └──────────────────┘                               │ → payment (simulated) │
                                                    │ → email (Mailpit)     │
                                                    └───────────────────────┘
```

## 2. Services

| Project | Responsibility | Data store |
|---|---|---|
| `MiniKobo.Catalog.Api` | Books, search, stock levels. Also an NServiceBus endpoint handling stock reservation. | RavenDB |
| `MiniKobo.Orders.Api` | Carts, placing orders, order status. | PostgreSQL (EF Core) |
| `MiniKobo.Orders.Worker` | Hosts the order-processing saga: stock → payment → email. | PostgreSQL (saga state + outbox) |
| `MiniKobo.Contracts` | Message classes (commands/events) shared between services. | – |
| `web/` | Next.js + TypeScript frontend. | – |

**Why separate services:** the two databases already split the business into two areas, *Catalog* and *Orders*. Each service owns its own data, and nothing else touches that data directly. Services communicate through HTTP (synchronous) or messages (asynchronous). This is the main lesson of the project.

**Why `Orders` (plural):** if a namespace `MiniKobo.Order` and a class `Order` both exist, C# can't tell which one `Order` refers to. Plural names for services and namespaces, singular names for entities, is the usual .NET convention.

## 3. Key decisions and the reasons for them

### 3.1 Catalog in RavenDB (document database)
A book is a self-contained aggregate that is read far more often than it is written. Storing it as one JSON document means a single read with no joins. RavenDB's indexes provide full-text search.

**Stock** is stored on the book document. Reservations use RavenDB **optimistic concurrency**: a write fails if the document changed since we read it, so two orders can't both take the last copy.

### 3.2 Orders in PostgreSQL with EF Core
Orders and carts need transactions and strict consistency, which is what a relational database provides.

**Why PostgreSQL instead of SQL Server (the original scope):** SQL Server images are only published for x86/amd64. On Apple Silicon they run under Rosetta emulation, which is slow and needs at least 2 GB of RAM, and Testcontainers would start extra instances on top of that. PostgreSQL runs natively on arm64, is lightweight, and is fully supported by both EF Core (the Npgsql provider) and NServiceBus SQL persistence (`SqlDialect.PostgreSql()`). The concepts we're learning (relational modelling, transactions, migrations, the outbox) are the same on both. Table names use `snake_case`, the PostgreSQL convention, mapped from C# `PascalCase` by a naming-convention package.

**Data access:** EF Core maps C# classes to tables, tracks changes, and versions the schema through **migrations** that we commit to git. We chose it over Dapper because it's the most common choice in the .NET ecosystem and saves us writing SQL by hand. Dapper remains an option for performance-critical queries.

### 3.3 Cart stored server-side
The cart lives in PostgreSQL, owned by the Orders API. A guest gets a cart id (a GUID) that the browser stores. This gives us practice with CRUD endpoints and EF Core before the asynchronous work starts. It also means a cart survives page refreshes.

### 3.4 Prices looked up from Catalog over HTTP
When an item is added to the cart, Orders calls the Catalog API through a typed `HttpClient` to get the book's current title and price. **The browser's price is never trusted.**

Alternative not chosen: keep a local copy of catalog data in Orders, synced through events. That removes the runtime dependency on Catalog, but it adds complexity we don't need yet.

### 3.5 Asynchronous checkout with NServiceBus over RabbitMQ
Checkout returns as soon as the order is saved. Processing then continues in the background, so a slow payment or a mail outage doesn't block the customer, and failed steps are retried automatically.

- **Outbox:** the order row and the `OrderPlaced` message are committed together. Without it, a crash between "save" and "publish" leaves an order that nobody processes, or a message about an order that doesn't exist.
- **Saga:** `OrderProcessingSaga` is a long-running workflow whose state is saved between messages. It coordinates the steps and runs **compensation** (releasing stock) when a later step fails.
- **At-least-once delivery:** messages can arrive more than once, so every handler must be **idempotent** (safe to run twice).

### 3.6 Messages

| Kind | Name | Sent by → handled by |
|---|---|---|
| Event | `OrderPlaced` | Orders.Api → Saga |
| Command | `ReserveStock` | Saga → Catalog |
| Event | `StockReserved` / `StockReservationFailed` | Catalog → Saga |
| Command | `ProcessPayment` | Saga → payment handler (simulated) |
| Event | `PaymentSucceeded` / `PaymentFailed` | payment handler → Saga |
| Command | `ReleaseStock` | Saga → Catalog (compensation) |
| Command | `SendConfirmationEmail` | Saga → email handler |

**Command** = "please do this", sent to exactly one receiver. **Event** = "this happened", published to any number of subscribers.

### 3.7 Order status lifecycle
```
Placed → StockReserved → Paid → Confirmed
   └→ Failed (out of stock)     └→ Failed (payment declined, stock released)
```

### 3.8 Status updates: polling first, then SignalR
The frontend first polls `GET /orders/{id}` every few seconds, which is simple and easy to test. In M8 we add **SignalR** to push status changes in real time, and keep polling as a fallback.

### 3.9 Email via Mailpit
The email handler sends real SMTP to a **Mailpit** container, a fake mail server with a web inbox. So we test real email-sending code without sending real email.

### 3.10 Guest checkout, no authentication
The customer provides a name and email at checkout. Authentication is out of scope, so the focus stays on the architecture.

### 3.11 Code organised by feature (vertical slices)
Inside each service, code is grouped by feature rather than by technical layer:

```
MiniKobo.Orders.Api/
  Features/
    Carts/
      AddCartItem.cs      // endpoint + request + handler for one use case
      GetCart.cs
    Orders/
      PlaceOrder.cs
      GetOrderStatus.cs
  Data/
    OrdersDbContext.cs
  Program.cs
```

Everything you need to understand one use case sits together. We chose this over Clean Architecture (separate Domain, Application and Infrastructure projects), which is common in enterprise .NET but too heavy for an app this size.

## 4. Testing strategy

| Layer | Tool | What it proves |
|---|---|---|
| Unit | NUnit (+ `NServiceBus.Testing` for the saga) | Logic in isolation: fast, no infrastructure. |
| Integration | Testcontainers | Our code against real RavenDB / PostgreSQL / RabbitMQ in throwaway Docker containers. |
| End-to-end | Playwright | The whole system through a real browser: search → cart → checkout → confirmed → email. |

## 5. Local development

- **During development:** infrastructure (RavenDB, PostgreSQL, RabbitMQ, Mailpit) runs in Docker Compose. The .NET services and Next.js run on the host with `dotnet run` / `npm run dev`, for fast debugging and hot reload.
- **From M7:** every service is containerised, and `docker compose up` starts the whole stack.

## 6. Repository conventions

- `Directory.Build.props`: settings shared by all projects (target framework, nullable, warnings as errors).
- `Directory.Packages.props`: Central Package Management (all NuGet versions in one place).
- `.editorconfig`: code style.
- `tests/Directory.Build.props`: imports the root file and adds the NUnit packages to every test project.
- Projects live in `src/`, tests in `tests/`, with one `*.UnitTests` and one `*.IntegrationTests` project per service.
- **Git:** one branch per milestone (for example `m1-catalog-api`), merged into `main` through a pull request. Each PR is the review checkpoint for that milestone.

## Decision log

| # | Decision | Chosen | Alternatives considered |
|---|---|---|---|
| 1 | Service split | Separate Catalog API, Orders API, Orders Worker | Modular monolith; fine-grained microservices |
| 2 | Cart storage | Server-side, PostgreSQL | Browser localStorage; RavenDB |
| 3 | Stock location | On the book document in RavenDB | Separate Inventory service |
| 4 | Orders data access | EF Core | Dapper |
| 5 | Status updates | Polling, then SignalR | SignalR/SSE from the start |
| 6 | Email | SMTP to Mailpit | Log only |
| 7 | Customers | Guest checkout, no auth | Accounts + auth |
| 8 | Code structure | Feature folders | Clean Architecture |
| 9 | Service naming | `Orders` (plural) | `Order` (namespace/class clash) |
| 10 | Cart pricing | Orders calls Catalog over HTTP | Local catalog copy synced through events |
| 11 | Git workflow | Branch per milestone + PR | Commit directly to `main` |
| 12 | Orders database | PostgreSQL (native arm64, lightweight). The scope originally said SQL Server; an optional final step switches the provider to practise SQL Server. | SQL Server under Rosetta (heavy on Apple Silicon); SQLite (no NServiceBus outbox/saga support); MySQL (community EF Core provider lags releases) |
