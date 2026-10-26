# MiniKobo Roadmap

Milestone plan for MiniKobo. The reasons behind each decision are in [ARCHITECTURE.md](ARCHITECTURE.md), and the concepts learned are in [LEARNING-NOTES.md](LEARNING-NOTES.md).

**How we work**
- Each milestone has its own branch and is merged into `main` through a pull request.
- Each milestone ends with a **review checkpoint**: we walk through the code and concepts before moving on.
- Tests are written within each milestone, not left until the end.

---

## M0 – Environment and repository foundations
Branch: `m0-foundations`

- [x] Verify tooling: .NET SDK 10.0.402, Docker 29.1.3 / Compose v2.40.3 (Docker Desktop), Node v24.3.0, VS Code
- [x] Git: settle on `main` as the default branch, make an initial commit, create a GitHub remote (`github.com/liaquatm/MiniKobo`)
- [x] Solution-wide settings: `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`
- [x] `docker-compose.yml` with RavenDB, PostgreSQL, RabbitMQ and Mailpit; secrets in `.env` (git-ignored) with a committed `.env.example`
- [x] Open each tool's web UI to confirm it is running
- [x] **Review checkpoint** (skipped by choice)

Concepts: solution/project structure, NuGet and Central Package Management, Docker Compose basics.

## M1 – Catalog API (RavenDB)
Branch: `m1-catalog-api`

- [x] Create `MiniKobo.Catalog.Api` (Minimal APIs) and add it to the solution
- [x] Register the RavenDB `DocumentStore` in DI, with a session per request
- [x] `Book` document and seed data
- [x] `GET /books` (paged) and `GET /books/{isbn}`
- [x] Full-text search index and `GET /books?search=…`
- [x] Configuration (`appsettings.json`, environment variables) and `ProblemDetails` errors
- [x] NUnit unit tests project
- [x] Testcontainers integration test against RavenDB
- [x] **Review checkpoint** (quiz)

Concepts: ASP.NET Core startup pipeline, DI lifetimes, document modelling, indexes.

## M2 – Orders API part 1: cart (PostgreSQL, EF Core)
Branch: `m2-orders-cart`

- [x] Create `MiniKobo.Orders.Api` (controller-based Web API)
- [x] `OrdersDbContext` (Npgsql provider, `snake_case` naming) with `Cart` / `CartItem` entities and the first migration
- [ ] `POST /carts`, `GET /carts/{id}`
- [ ] `PUT /carts/{id}/items/{bookId}` (set quantity), `DELETE /carts/{id}/items/{bookId}`
- [ ] Typed `HttpClient` to the Catalog API for title and price
- [ ] Request validation
- [ ] Unit tests and a PostgreSQL Testcontainers integration test
- [ ] **Review checkpoint**

Concepts: EF Core change tracking and migrations, `HttpClientFactory`, validation.

## M3 – Checkout and messaging foundations
Branch: `m3-checkout-messaging`

- [ ] Create `MiniKobo.Contracts` with the message classes
- [ ] NServiceBus in the Orders API: RabbitMQ transport, SQL persistence (PostgreSQL dialect), outbox
- [ ] `Order` entity and migration
- [ ] `POST /orders`: cart → `Order` (status `Placed`) and publish `OrderPlaced`
- [ ] `GET /orders/{id}` for status
- [ ] A temporary logging handler; watch the message in the RabbitMQ UI
- [ ] **Review checkpoint**

Concepts: queues and exchanges, commands vs events, the outbox pattern.

## M4 – Order-processing saga
Branch: `m4-order-saga`

- [ ] Create `MiniKobo.Orders.Worker` hosting `OrderProcessingSaga`
- [ ] Catalog API as an NServiceBus endpoint: `ReserveStock` / `ReleaseStock` handlers using optimistic concurrency
- [ ] Simulated payment with a configurable failure rate
- [ ] `SendConfirmationEmail` handler sending SMTP to Mailpit
- [ ] Order status updated at each step, with compensation on failure
- [ ] Retries, error queue, and idempotent handlers
- [ ] Saga unit tests with `NServiceBus.Testing`
- [ ] **Review checkpoint**

Concepts: sagas, compensation, at-least-once delivery, eventual consistency.

## M5 – Next.js frontend
Branch: `m5-frontend`

- [ ] Create the Next.js (App Router, TypeScript) app in `web/`
- [ ] Decide: browser calls the APIs directly (CORS) or through Next.js server code
- [ ] Catalog page with search, and a book details page
- [ ] Cart page (change quantities, remove items)
- [ ] Checkout form (name and email)
- [ ] Order status page with polling
- [ ] **Review checkpoint**

Concepts: React components, server vs client components, TypeScript types for API DTOs.

## M5b – Customer accounts (ASP.NET Core Identity)
Branch: `m5b-accounts`

- [ ] ASP.NET Core Identity in the Orders API (`MapIdentityApi`), with users stored in PostgreSQL through EF Core
- [ ] Register, login and logout, and protect endpoints with `[Authorize]`
- [ ] Link carts and orders to the signed-in customer, and merge a guest cart into the account on login
- [ ] Keep guest checkout working
- [ ] Frontend: login and register pages, and a "My orders" page
- [ ] Tests with and without a signed-in user
- [ ] **Review checkpoint**

Concepts: authentication vs authorisation, cookies vs bearer tokens, claims, password hashing.

## M6 – End-to-end testing
Branch: `m6-e2e-tests`

- [ ] Playwright setup
- [ ] Happy path: search → add to cart → checkout → `Confirmed` → email in Mailpit
- [ ] (Optional) Unhappy path: payment fails and stock is released
- [ ] Review the overall test strategy
- [ ] **Review checkpoint**

## M7 – Containerisation and CI
Branch: `m7-docker-ci`

- [ ] Multi-stage Dockerfiles for each .NET service and for `web/`
- [ ] The full stack starts with `docker compose up`
- [ ] GitHub Actions: build → unit tests → integration tests → Playwright
- [ ] **Review checkpoint**

Concepts: container images, CI pipelines, caching.

## M8 – Real-time status (SignalR)
Branch: `m8-signalr`

- [ ] Compare SignalR vs Server-Sent Events
- [ ] Push order status changes to the browser
- [ ] Keep polling as a fallback
- [ ] **Review checkpoint**

## Final – Retrospective
- [ ] Walk through the full architecture
- [ ] What worked, what we'd change
- [ ] What production would need: auth, observability, scaling, real payments
- [ ] (Optional) Switch the Orders EF Core provider and NServiceBus dialect to SQL Server, to practise SQL Server and see how provider-agnostic the code really is
