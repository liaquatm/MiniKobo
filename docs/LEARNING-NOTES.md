# MiniKobo Learning Notes

A short reference covering **concepts and patterns**, plus the reasons behind them. Exact syntax lives in the code and the official docs, so look it up when needed. The design decisions themselves are recorded in [ARCHITECTURE.md](ARCHITECTURE.md).

---

## M0 – Environment and foundations

### Build configuration
- **`.slnx` solution:** the list of projects that belong together (the XML format, default from .NET 10).
- **`.csproj` is the source of truth.** Unlike Unity, nothing regenerates it. `Sdk="Microsoft.NET.Sdk.Web"` brings in all of ASP.NET Core with no NuGet package.
- **`Directory.Build.props`:** MSBuild imports it automatically into every project below it. Shared settings live here: `TargetFramework`, `Nullable`, `ImplicitUsings`, `TreatWarningsAsErrors`. A project's own value overrides it, so don't duplicate settings in the `.csproj`.
- **`Directory.Packages.props`:** Central Package Management. Versions live here, and the `.csproj` lists only package names. `dotnet add package` writes the version here automatically.
- **`.editorconfig`:** code style rules read by the IDE and compiler. `root = true` stops the search for other `.editorconfig` files in parent folders.
- **Nullable reference types:** `string` means never null, `string?` means may be null. The compiler warns about possible null dereferences, and with warnings-as-errors these become build errors. `!` (null-forgiving) switches the check off, so it needs a reason.

### Docker Compose
- **Describe containers in YAML and start or stop them together.** `docker compose up -d`, `ps`, `logs <service>`, `stop`, `down` (removes containers), `down -v` (also deletes the data).
- **`ports: "HOST:CONTAINER"`** lets code on the Mac reach a service inside a container.
- **Volumes** keep data outside the disposable container.
- **Multi-arch images:** a tag that points to a manifest list, so Docker picks the build matching your CPU (arm64 on the M2, amd64 in CI). Check that a tag lists both before using it.
- **Secrets go in `.env`** (git-ignored), with a committed `.env.example` documenting the variables. `${VAR:?message}` makes Compose stop if the variable is missing.

### Git workflow
- **One branch per milestone**, merged into `main` with a pull request, which doubles as the review.
- **Commit small, working steps**, one logical change each. Stage files by name instead of `git add .`.
- `git switch -c` creates a branch and switches to it. Git refuses to switch if that would overwrite uncommitted changes; use `git stash` / `git stash pop` to park them.
- `git commit --amend` rewrites the last commit. Only do this before pushing.

---

## M1 – Catalog API (RavenDB)

### The ASP.NET Core host (`Program.cs`)
Every app has two phases:
1. **Builder (setup):** `WebApplication.CreateBuilder` sets up configuration, logging, the **DI container** (`builder.Services`) and the Kestrel web server. **Register services here.**
2. **`builder.Build()`** locks the container and creates the app.
3. **App (handling requests):** add middleware and map endpoints, then `app.Run()` blocks and serves.

Top-level statements mean there's no `Main` method; the compiler generates it, and `await` works at the top level.

### Configuration
- **Layered, with later sources overriding earlier ones:** `appsettings.json` → `appsettings.{Environment}.json` → environment variables → command-line args.
- **Environment variables** express nesting with a double underscore: `RavenDb__Urls__0`. This is how Docker and CI override settings.
- **`ASPNETCORE_ENVIRONMENT`** (usually `Development` locally) selects the environment-specific file and turns development-only behaviour, such as seeding, on or off.
- **`launchSettings.json`** is used only by `dotnet run` and IDEs locally, never deployed.
- **The Options pattern:** bind a config section to a typed class (`AddOptions<T>().BindConfiguration(...)`), validate it with data annotations, and use `ValidateOnStart()` so bad config fails at startup.

### Dependency injection lifetimes
| Lifetime | One instance per… | Example here |
|---|---|---|
| **Singleton** | application | `IDocumentStore`: expensive, thread-safe, holds connection pools |
| **Scoped** | HTTP request | `IAsyncDocumentSession`: the per-request unit of work |
| **Transient** | every time it's requested | (none yet) |

- **Registration through an extension method:** `AddRavenDb(this IServiceCollection services)` keeps `Program.cs` short and the setup in one place.
- **A factory lambda** (`sp => …`) builds a service the first time it's requested and can resolve other services.
- **Outside a request** (for example at startup) there's no scope, so open what you need directly, such as `store.OpenAsyncSession()`.

### RavenDB essentials
- **Documents are plain C# classes stored as JSON.** No attributes, no base class. The collection name comes from the class name, pluralised (`Book` → `Books`).
- **IDs are strings with a collection prefix** (`books/9780000000001`). Choosing meaningful IDs makes lookups predictable.
- **The session is a unit of work:** load and store objects, then call `SaveChangesAsync()` to write all changes in one transaction.
- **Load by ID** (`LoadAsync`) is fast and never uses an index, so it's never stale.
- **Queries always use an index.** If none matches, RavenDB creates an **auto-index**. Indexes update in the background, so they can be briefly **stale**.
- The database must exist before use; RavenDB doesn't create it automatically.

### Modelling and C# idioms
- **`required`** forces a property to be set when the object is created. **`init`** means it can only be set during creation; use `set` for values that change, such as stock.
- **`decimal` for money**, never `float`/`double`, because binary floating point can't represent 0.1 exactly.
- **`record`** is for data carriers: one line gives you a constructor, immutable properties and value equality. Ideal for DTOs.
- **`sealed` by default** for classes not designed for inheritance.
- Also used: file-scoped namespaces (`namespace X;`), collection expressions (`[]`), expression-bodied members (`=>`), and pattern matching (`x is < 1 or > 50`).

### Idempotency
**Running an operation more than once has the same effect as running it once.** The seeder loads the seed IDs and stores only the missing books, so restarts never duplicate data or reset stock. This becomes essential for message handlers in M4.

### Minimal API endpoints
- **One file per endpoint** in a feature folder, containing a `MapXyz` extension method and a `public` `HandleAsync`, which can be unit-tested directly.
- **Route groups:** `MapGroup("/books")` gives a shared prefix and shared settings.
- **Parameter binding:**
  - a route placeholder `{isbn}` binds to a route parameter,
  - registered types come from **DI**,
  - `CancellationToken` comes from the **request** (cancelled if the client disconnects),
  - other simple types come from the **query string**, with defaults making them optional.
- **Typed results:** `Results<Ok<T>, NotFound>` declares the possible responses in the method signature. The compiler enforces it, and documentation tools can read it.
- **Response DTOs instead of documents:** the API shape is a contract, so internal fields never leak out.
- **The public identifier (ISBN) is not the storage ID:** URLs shouldn't depend on how data is stored.
- **Paging:** always use a fixed sort order, cap the page size, and return the total count (RavenDB `Statistics(out var stats)`).
- **ProblemDetails (RFC 9457):** the standard JSON shape for errors. `TypedResults.ValidationProblem(...)` returns 400 with field errors.
- **JSON uses camelCase by default**, regardless of PascalCase C# names.

### Middleware and consistent errors
- **Middleware is an onion:** each one wraps everything registered after it. A request goes in through each layer, and the response comes back out in reverse. **Order matters**, which is why the exception handler goes first.
- `AddProblemDetails()` + `UseExceptionHandler()` + `UseStatusCodePages()` make **every** error (validation, 404, unhandled exception) use the same ProblemDetails JSON. Exception details go to the logs, never to the caller.

### RavenDB indexes and search
- **Static index** (`AbstractIndexCreationTask`): `Map` picks the fields to index. It runs on the **server**. `Books_Search` becomes `Books/Search`.
- **`FieldIndexing.Search`** runs text through an **analyzer** (split into words, lowercased), so search matches single words and ignores case.
- **`IndexCreation.CreateIndexes(assembly, store)`** deploys every index class at startup, and does nothing if they're unchanged.
- **Deferred execution:** a LINQ query is only a description until `ToListAsync()`, so conditions can be added step by step.
- **Eventual consistency:** indexes catch up in the background, so a query straight after a write can be **stale**. Fast answers are traded for up-to-date ones. Loads by ID are always current. Tests must wait for indexing.

### Testing strategy
| | Unit tests | Integration tests |
|---|---|---|
| Tests | pure logic, in isolation | our code + real infrastructure + the HTTP pipeline |
| Speed | milliseconds | seconds (container start) |
| Needs Docker | no | yes |
| Catches | logic bugs | wiring, config, queries, serialisation, status codes |

- **Separate projects** (`*.UnitTests`, `*.IntegrationTests`) so the fast tests can run on their own (in CI, or before every commit).
- **An API that mostly does I/O has little pure logic.** Most of its value is tested at the integration level. Don't invent complexity to "have unit tests".
- **Design for testability ("functional core, imperative shell"):** keep pure logic (plain inputs and outputs) separate from I/O (database, network). `ValidatePaging(int, int)` needs no database, so it gets fast unit tests. `HandleAsync` needs a session, so it's covered by integration tests. Testing logic buried in I/O code means `null!` hacks or large mocks.
- **`InternalsVisibleTo`** lets a test project see `internal` members without making them `public`.
- **`WebApplicationFactory<Program>`** runs the **real** API in memory, with the real `Program.cs`, DI and middleware, and gives you an `HttpClient` to call it. `UseSetting` overrides configuration, just like an environment variable. It runs in the **`Development` environment by default**, which is why our dev-only seeder fills the test database. When integration tests behave strangely, check which environment the app is running in.
- **Testcontainers** starts a throwaway Docker container per test run, so you test against the real database version, and every run starts clean.
- **NUnit:** `[TestFixture]` is a class of tests, `[Test]` a single test, and `[TestCase(...)]` one test run with several inputs. `[OneTimeSetUp]`/`[OneTimeTearDown]` run once per fixture, which is used for expensive things like containers. Assertions use the constraint model: `Assert.That(actual, Is.EqualTo(expected))`. `Assert.Multiple` reports every failing assertion, not just the first.
- **Nested `Directory.Build.props`:** MSBuild only picks up the *nearest* one, so `tests/Directory.Build.props` imports the root one explicitly and then adds the test packages for every test project.

---

## M2 – Orders API (PostgreSQL, EF Core, controllers)

### What EF Core is
- **The problem:** code works with **objects**, and the database stores **tables**. Something has to translate between them. By hand, that means SQL strings plus copying every column into objects and back, for every query.
- **EF Core is an ORM (Object-Relational Mapper):** you query with LINQ on C# objects. EF Core **generates the SQL**, runs it, **builds the objects**, and on `SaveChangesAsync()` works out the `INSERT`/`UPDATE`/`DELETE` statements itself. (Dapper, the lighter alternative, only maps results to objects; you still write the SQL.)
- **Unity comparison:** like Unity serialising your fields to scene files without you writing the file format, but with a database instead of files.

| Piece | What it is | Ours |
|---|---|---|
| Entities | The classes that get stored | `Cart`, `CartItem` |
| `DbContext` | Queries and saves, and tracks changes (unit of work) | `OrdersDbContext` |
| Mapping | How classes become tables (names, keys, lengths) | `OnModelCreating` |
| Provider | Speaks one database's SQL dialect | `UseNpgsql` |

- **Migrations are version control for the database's structure:** ordered C# files, each describing one schema change (`Up` applies, `Down` reverses), committed to git. Any database can be brought up to date by applying the ones it hasn't had (tracked in `__EFMigrationsHistory`).
- **The library vs the tool:** **EF Core** (the library) runs inside the app. **`dotnet ef`** is a separate developer tool for migrations. To read the model, it builds the project and runs the app's setup code to get the `DbContext`. That's why the app's configuration must work, for example a connection string must be present, even though `migrations add` doesn't connect to the database.
- **Never edit an applied migration.** The database has already recorded it as applied and will skip it, and databases would drift apart. Add a new migration instead, the same rule as never amending pushed git commits.

### Tooling and secrets
- **Local .NET tools** (`dotnet-tools.json` at the repo root): command-line tools pinned per repo. `dotnet tool restore` installs them on any machine or in CI.
- **EF Core packages must share a major version** with each other and with .NET (all 10.x here). The `Design` package is a development-time dependency (`PrivateAssets=all`).
- **User secrets:** `dotnet user-secrets init` adds a `UserSecretsId` to the `.csproj`. Values are stored in `~/.microsoft/usersecrets/<id>/secrets.json`, outside the repo. In `Development` they become another configuration layer that overrides `appsettings.json`. Use them for local passwords and connection strings.

### EF Core basics
- **`DbContext` is the unit of work** (like RavenDB's session): it tracks loaded entities, and `SaveChangesAsync()` writes the changes in one transaction. `AddDbContext` registers it as scoped.
- **Provider:** `UseNpgsql(...)` decides which database the SQL is written for. Most of the rest of the code doesn't depend on the database.
- **Fluent API mapping** in `OnModelCreating`: `ToTable`, `HasKey`, `HasMany().WithOne().HasForeignKey()`, `HasMaxLength`, `HasPrecision`. It keeps database concerns out of the entity classes.
- **Conventions vs explicit config:** EF names a table after the `DbSet` property if there is one, otherwise after the class. That produced `carts` and `cart_item`, so we set names explicitly with `ToTable`. Be explicit wherever a convention gives a surprising result.
- **A composite key** (`HasKey(i => new { i.CartId, i.Isbn })`) makes the database enforce a uniqueness rule: one row per book per cart.
- **Aggregates (DDD):** expose only the root (`DbSet<Cart>`). Child entities (`CartItem`) are reached and changed only through the root, so its rules can't be bypassed.

### Rich domain model
- **Entities protect their own state:** private setters, a private `List<T>` field exposed as `IReadOnlyList<T>`. Changes go through methods that enforce the rules.
- **EF Core still works with this:** it uses constructors whose parameter names match properties, sets private setters, and fills backing fields (`Items` → `_items`) by convention.
- **Snapshot data:** a cart item copies the title and price at the time it's added. It's checked again at checkout, because a stored price is never trusted for payment.
- **IDs and time:** a `Guid` cart ID can't be guessed (unlike 1, 2, 3). `DateTimeOffset` maps to `timestamptz` and avoids `DateTime`'s time-zone ambiguity.

### Migrations
- **`dotnet ef migrations add <Name>`** compares the model with the last **snapshot** and generates C# with `Up()` (apply) and `Down()` (reverse). It builds and starts the app's host to read the model, but doesn't touch the database.
- **`dotnet ef database update`** applies pending migrations. Applied migrations are recorded in the `__EFMigrationsHistory` table.
- **Review the generated migration before applying it.** That's where we caught the inconsistent table name.
- **`dotnet ef migrations remove`** deletes the latest migration **if it hasn't been applied**. Once a migration has been applied or shared, never edit it; add a new migration instead.
- Commit the migrations **and** the `ModelSnapshot` file.
- **`dotnet ef migrations has-pending-model-changes`** checks whether the model has changed since the last migration. Computed get-only properties (`Total`, `LineTotal`) aren't mapped, so they need no migration.

### Controllers (vs Minimal APIs)
| | Minimal APIs (Catalog) | Controllers (Orders) |
|---|---|---|
| Registration | `MapGet(...)` per endpoint | `AddControllers()` + `MapControllers()` discover classes |
| Routes | In the `Map` call | Attributes: `[Route("carts")]` on the class, `[HttpPut("{cartId:guid}/items/{isbn}")]` on actions |
| Dependencies | Handler parameters | Constructor (here a primary constructor), shared by all actions |
| Results | `TypedResults.Ok(...)`, `Results<...>` | `ActionResult<T>`: return a `T` (200) or a helper such as `NotFound()`, `Problem(...)`, `NoContent()`, `CreatedAtAction(...)` |
| Validation | Manual | **`[ApiController]` validates automatically** and returns 400 ProblemDetails before the action runs |

- **`[ApiController]`** also infers binding sources: complex types come from the **body**, and simple types from the route or query string.
- **Route constraints** (`{cartId:guid}`): a value that isn't a GUID doesn't match the route, so the result is a 404 and the action never runs.
- **`CreatedAtAction(nameof(GetCart), new { cartId }, body)`** returns 201 with a `Location` header built from another action's route. `nameof` keeps it refactor-safe. Avoid an `Async` suffix on action names, because MVC trims it by default and `CreatedAtAction` then can't find the action.
- **Data annotations on records:** put attributes on the constructor parameter (`[Range(1, 10)] int Quantity`).
- **`Include(cart => cart.Items)`** is **eager loading**. EF loads related data only when asked, and without it `Items` would be empty.

### HTTP status codes used
- **201** created (with `Location`), **204** success with no body, **400** malformed or invalid request.
- **404** not found. Use a distinct `title` when several things could be missing (cart vs book vs item).
- **409 Conflict**: valid request, but it conflicts with the current state (not enough stock).
- **503 Service Unavailable**: a dependency is down, so try again later.

### Calling another service
- **Typed `HttpClient`** (`AddHttpClient<ICatalogClient, CatalogClient>`): **`IHttpClientFactory`** creates and pools the underlying connections. Never `new HttpClient()` per request, which exhausts sockets.
- **An interface (`ICatalogClient`)** lets tests substitute a fake.
- **Each service has its own copy of the data it reads** (`CatalogBook`, with only the fields it needs). There's no shared code between services, so each can change and deploy independently. Extra JSON fields are ignored.
- **Translate failures at the boundary:** the client turns network problems into a `CatalogUnavailableException`, and the controller turns that into a 503. Callers don't deal with HTTP details.
- **Resilience (`AddStandardResilienceHandler`, built on Polly):** retries with exponential backoff and jitter for brief failures, timeouts, and a **circuit breaker** that stops calling a failing service for a while so it can recover. A 404 isn't retried, because retrying can't fix it. It's only safe to retry operations that can be repeated without harm (a GET).

### Testable time and logging
- **`TimeProvider`** (registered as `TimeProvider.System`): inject the clock instead of calling `DateTimeOffset.UtcNow`, so tests can control "now".
- **`Guid.CreateVersion7()`**: GUIDs that start with a timestamp, so they sort by creation time. That's kinder to database indexes than random GUIDs.
- **Structured logging:** `logger.LogWarning(ex, "… {Isbn} … {CartId}", isbn, cartId)`. The placeholders become **named fields**, which can be searched in log tools ("all failures for cart X"). Don't build the message with string interpolation.
- **Defence in depth:** the controller validates the quantity (400), and `Cart` checks it again (it throws). The entity protects its own rules whoever calls it.
