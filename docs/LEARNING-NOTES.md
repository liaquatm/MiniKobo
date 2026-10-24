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
