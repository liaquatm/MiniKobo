# MiniKobo: Goal and Tech Stack

## Goal

Build **MiniKobo**, a small full-stack online bookstore.

It should be a working e-commerce app where a customer can:

- browse and search a catalog of books,
- add books to a cart and adjust quantities,
- check out, which places an order and triggers asynchronous back-end processing (stock reservation, simulated payment, confirmation email),
- follow the order's status as it moves through processing.

The emphasis is on **clean, readable code and sound architecture**, not on feature volume.

## Tech stack

| Area | Technology |
|---|---|
| Back end | ASP.NET Core Web API (C#), .NET 10 |
| Catalog database | RavenDB (Docker) |
| Orders database | SQL Server (Docker), with EF Core or Dapper |
| Messaging | RabbitMQ with NServiceBus |
| Front end | Next.js + TypeScript |
| Unit tests | NUnit |
| Integration tests | Testcontainers |
| End-to-end test | Playwright |
| Local environment | Docker Compose |
| CI | GitHub Actions |
