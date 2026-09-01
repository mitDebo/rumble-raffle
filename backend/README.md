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
  reachable. As SignalR and image storage land later in Phase 1, their
  checks get registered with the `"ready"` tag and fold into `/ready`
  automatically — no changes needed to `Program.cs` when that happens.

`src/RumbleRaffle.Core` holds everything that touches an external system
directly, organized by concern in its own folder: `Database/` currently has
`RumbleRaffleDbContext`, its `Migrations/`, and connection-string handling
(`ConnectionStrings.Resolve`/`Normalize`); future auth- and storage-related
classes get their own `Auth/`/`Storage/` folders alongside it rather than
piling into `Database/`. `ServiceCollectionExtensions.cs` stays at the
project root as Core's single composition entry point
(`AddRumbleRaffleCore()`) — `Program.cs` calls it instead of touching EF
Core directly, and it'll grow to wire up those future folders too without
Program.cs needing to change. `src/RumbleRaffle.Api` is a thin composition
root: `Program.cs` wires Core in and maps endpoints; any future MVC
controllers go in `Controllers/`. If any of Core's contents ever need to be
testable with zero database dependency, it can split into a separate
`RumbleRaffle.Infrastructure` project later without disturbing
`RumbleRaffle.Api`.

Open `RumbleRaffle.slnx` (all four projects) rather than targeting each
project path individually.

## Configuration

Copy `.env.example` to `.env` in this directory and fill in real values
(never committed — it's gitignored). `Program.cs` loads it automatically via
`DotNetEnv` on startup; in production, Docker Compose supplies the same
variables as real environment variables instead, and no `.env` file is
present in the container.

`ConnectionStrings__Default` accepts either a `postgresql://` URI (what
Supabase's dashboard hands out) or a standard Npgsql keyword=value string —
`Program.cs` normalizes the URI form before handing it to Npgsql.

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

The unit test project is still a placeholder — nothing in the codebase yet
warrants a real unit test (see NFR-7 in the spec for the reasoning). The
integration tests are real: `HealthEndpointsTests` proves `/health` and
`/startup` respond correctly with nothing registered, and
`ReadyEndpointDatabaseTests` proves `/ready`'s database check actually works
against a real, ephemeral Postgres instance spun up via Testcontainers for
the duration of that test and torn down afterward — never against the real
Supabase project. Testcontainers needs Docker running locally (already true
on this machine) and works out of the box on GitHub Actions' hosted runners.

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
