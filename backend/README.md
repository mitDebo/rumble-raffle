# Rumble Raffle — Backend

ASP.NET Core Web API. Exposes three Kubernetes-style health endpoints
(`/health`, `/ready`, `/startup`) via ASP.NET Core's built-in health-check
middleware, and is wired up to Postgres (Supabase) via EF Core. This
project doesn't run on Kubernetes today (it's Docker Compose on a single
droplet), but these are built in from the start in case that changes later.

- `/health` (liveness) has no checks registered — it should only ever
  confirm the process itself is responsive, never depend on external
  services like the database.
- `/ready` (readiness) and `/startup` filter registered health checks by the
  `"ready"`/`"startup"` tags. `/ready` currently checks that the database is
  reachable. Image storage (1.9) deliberately doesn't have a `"ready"` check
  yet, even though `IImageStorage`/`SupabaseImageStorage` now exist — no
  real bucket or API key is provisioned until 5.2 actually needs one, and a
  check that always reports Unhealthy for a dependency nothing uses yet
  would make `/ready` less accurate, not more. Add one alongside 5.2's work
  once the key is real. SignalR deliberately never gets one at all: it's
  in-process middleware here (no Redis/Azure SignalR backplane), so there's
  no external dependency to probe — if the process is up, it's ready. That
  could change if a backplane gets added later for horizontal scaling; the
  backplane itself would be what's worth checking, not "SignalR" as a
  concept.

All three respond with JSON naming each registered check, not just the
aggregate status, e.g. `{"status":"Healthy","checks":[{"name":"postgres","status":"Healthy"}]}`.
The HTTP status code still reflects the aggregate (200 for Healthy/Degraded,
503 for Unhealthy — ASP.NET Core's default `ResultStatusCodes`). Deliberately
doesn't include each check's description/exception text, since that can
carry details (e.g. connection info in a failed Postgres check) that
shouldn't be exposed over an unauthenticated endpoint — check server-side
logs for that.

The endpoint mapping and response formatting live in
`src/RumbleRaffle.Api/Endpoints/` (`HealthCheckEndpoints.cs`,
`MapRumbleRaffleHealthChecks()`), separate from *what* gets checked:
registering a new check (`AddHealthChecks().AddNpgSql(...)` and whatever
SignalR/storage checks join it later) still happens in `Program.cs`, since
that's a composition-root decision, not an HTTP-layer one.

`src/RumbleRaffle.Core` holds everything that touches an external system
directly, organized by concern in its own folder: `Database/` has
`RumbleRaffleDbContext`, its `Migrations/`, connection-string handling
(`ConnectionStrings.Resolve`/`Normalize`), and entity configuration
(`Database/Configurations/`, one `IEntityTypeConfiguration<T>` per entity,
kept separate from the entity classes themselves); `Entities/` has the
plain entity classes (`User`/`UserFlags` as of 1.6); `Storage/` has
`IImageStorage` (the app-owned abstraction) and `SupabaseImageStorage` (its
real implementation, talking to Supabase Storage's REST API directly via a
typed `HttpClient` rather than a third-party SDK); `Auth/` has
`JwksOnlyConfigurationRetriever` (see "JWT validation" below).
`ServiceCollectionExtensions.cs` stays at the project root as Core's single
composition entry point (`AddRumbleRaffleCore()`) — `Program.cs` calls it
instead of touching EF Core, `HttpClient`, or JWT bearer auth directly.
`src/RumbleRaffle.Api` is a thin composition root: `Program.cs` wires Core
in, registers what gets health-checked, calls `UseAuthentication()`/
`UseAuthorization()`, and maps every endpoint. Api is deliberately laid out
differently than Core: instead of folders per concern, it uses the more
traditional ASP.NET Core layout, folders per *technical role*. `Endpoints/`
holds every minimal-API endpoint-mapping class and SignalR hub — one file
per feature (`HealthCheckEndpoints.cs`, `PingHub.cs`, `UserEndpoints.cs`
today; a feature never spans multiple files, but the folder they all live
in is chosen by "what kind of thing is this", not "what feature is this
about"). `Controllers/` is reserved for real MVC-style controllers
(`ControllerBase` subclasses) once one is actually needed — still just a
`.gitkeep` today. If any of Core's contents ever need to be testable with
zero database dependency, it can split into a separate
`RumbleRaffle.Infrastructure` project later without disturbing
`RumbleRaffle.Api`.

Table/column naming is snake_case project-wide (`EFCore.NamingConventions`,
`.UseSnakeCaseNamingConvention()` in `AddRumbleRaffleCore()`), matching
Postgres/Supabase's own convention (`auth.users` itself uses e.g.
`created_at`) rather than defaulting to PascalCase and needing manual
quoting in hand-written SQL. Added alongside `users` (1.6), the first real
domain table — nothing before it needed a convention decided.

### JWT validation

Supabase's own `auth.users`/`auth.identities` tables hold sign-in identity;
this app's `users` table is a 1:1 companion row (same `id`, populated by a
Postgres trigger — not yet written, see 1.7) holding only what the app
needs independently (`display_name`, `avatar_url`, `email`, the `flags`
bitmask). Protected endpoints validate the bearer JWT Supabase issues
against Supabase's JWKS, rather than trusting any shared secret.

Supabase doesn't yet expose a standard OIDC discovery document
(`/.well-known/openid-configuration`) — confirmed still in progress, not
shipped, via Supabase's own team — so the usual `JwtBearerOptions.Authority`
auto-configuration path doesn't work here. `Auth/JwksOnlyConfigurationRetriever`
bridges that gap: it fetches the bare JWKS document directly
(`{Supabase:Url}/auth/v1/.well-known/jwks.json`) and wraps it in the
`OpenIdConnectConfiguration` shape the JwtBearer handler expects, so
`ConfigurationManager` still provides its normal caching, periodic refresh,
and automatic forced refresh if a token's `kid` isn't recognized (relevant
if Supabase ever rotates its signing keys). Delete this and switch back to
plain `Authority`-based configuration once Supabase ships real OIDC
discovery. Validates issuer (`{Supabase:Url}/auth/v1`) and audience
(`"authenticated"`, Supabase's convention for any signed-in user regardless
of provider or magic link).

Open `RumbleRaffle.slnx` (all four projects) rather than targeting each
project path individually.

Both test projects mirror this layout rather than having their own
structure. A test's folder should match where the thing it tests lives:
something testing a class under `RumbleRaffle.Core/Database/` goes in a
`Core/Database/` folder in the test project (i.e. Core's contents get a
`Core/` wrapper folder in the tests, since Core is its own project); a test
for something in `RumbleRaffle.Api/Endpoints/` just goes in `Endpoints/`
(no extra wrapper, since the test project already belongs to Api — folder
maps straight to folder). Anything that isn't itself a test — `WebApplicationFactory`
subclasses, shared fixtures, assertion helpers — goes in `Scaffolding/` (for
things that stand up the app/environment under test, e.g. the `*ApiFactory`
classes) or `Util/` (for plain helper code), not alongside the tests
themselves.

## Configuration

Copy `.env.example` to `.env` in this directory and fill in real values
(never committed — it's gitignored). `Program.cs` loads it automatically via
`DotNetEnv` on startup; in production, Docker Compose supplies the same
variables as real environment variables instead, and no `.env` file is
present in the container.

`ConnectionStrings__Default` accepts either a `postgresql://` URI (what
Supabase's dashboard hands out) or a standard Npgsql keyword=value string —
`Program.cs` normalizes the URI form before handing it to Npgsql.

`Supabase__StorageSecretKey` can stay blank locally until 5.2 actually
needs a real upload to work against — `SupabaseImageStorage` doesn't
require it to be set at startup, only when a request actually goes out.

## Run it locally

```bash
dotnet run --project src/RumbleRaffle.Api
```

Then hit `http://localhost:<port>/health` (or `/ready`, `/startup`) — the
port is printed on startup.

## Run the tests

```bash
dotnet test tests/RumbleRaffle.Api.UnitTests
dotnet test tests/RumbleRaffle.Api.IntegrationTests
```

The unit test project has its first real test as of 1.9: `ImageStorageTests`
proves `IImageStorage`'s upload/`GetUrl` contract holds, via
`FakeImageStorage` rather than a real Supabase Storage call (there's no
network-reachable dependency to unit test against). The integration tests
are real: `HealthEndpointsTests` proves `/health` and
`/startup` respond correctly with nothing registered, and
`ReadyEndpointDatabaseTests` proves `/ready`'s database check actually works
against a real, ephemeral Postgres instance spun up via Testcontainers for
the duration of that test and torn down afterward — never against the real
Supabase project. Testcontainers needs Docker running locally (already true
on this machine) and works out of the box on GitHub Actions' hosted runners.
`MeEndpointTests` (1.6) proves `/api/users/me` rejects an unauthenticated
request with 401 — the accept path (a real, valid Supabase JWT) isn't
provable yet and is deferred to 1.7, once a real sign-in flow exists to
produce one.

## Migrations

```bash
dotnet ef migrations add <Name> --project src/RumbleRaffle.Core --startup-project src/RumbleRaffle.Api
dotnet ef database update --project src/RumbleRaffle.Core --startup-project src/RumbleRaffle.Api
```

`--project` points at Core since that's where `RumbleRaffleDbContext` and
`Migrations/` live; `--startup-project` still points at Api, since that's
the project that actually wires up configuration and DI (`Program.cs`) for
the EF Core tooling to resolve against. (These commands used to target just
`--project src/RumbleRaffle.Api`, from before `RumbleRaffleDbContext` moved
into Core — update this note if that ever moves again.)

Requires the `dotnet-ef` tool (`dotnet tool install --global dotnet-ef` if
you don't already have it). EF Core owns the schema exclusively — Supabase's
own GitHub/CLI migration integration is intentionally not used, to avoid two
systems both trying to own the same schema.

CI validates every migration bundle against a scratch Postgres before
anything ships (see `backend-migrate` in `.github/workflows/ci-cd.yml`) —
that's a pipeline gate against a throwaway database, not production.
Production itself gets migrated by the `deploy` job's "Apply migrations to
production" step, which runs `dotnet ef database update` directly against
Supabase (`secrets.SUPABASE_DB_CONNECTION_STRING`) before the droplet's
containers get restarted.
