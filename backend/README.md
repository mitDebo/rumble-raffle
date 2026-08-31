# Rumble Raffle — Backend

ASP.NET Core Web API. Currently exposes three Kubernetes-style health
endpoints (`/health`, `/ready`, `/startup`) via ASP.NET Core's built-in
health-check middleware, plus placeholder unit and integration test
projects proving out the build/test/deploy pipeline before any real feature
work starts. This project doesn't run on Kubernetes today (it's Docker
Compose on a single droplet), but these are built in from the start in case
that changes later.

- `/health` (liveness) has no checks registered — it should only ever
  confirm the process itself is responsive, never depend on external
  services like the database.
- `/ready` (readiness) and `/startup` filter registered health checks by the
  `"ready"`/`"startup"` tags. As the database, SignalR, and image storage
  land later in Phase 1, their checks get registered with the `"ready"` tag
  and fold into `/ready` automatically — no changes needed to `Program.cs`
  when that happens.

No `.sln` file yet — commands below target each project directly. One will be
added once there's more than a couple of projects to justify it.

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

Both are placeholder `Assert.True(true)` tests for now — they exist to prove
the CI pipeline actually runs a unit-test step and an integration-test step,
not to cover real behavior yet.
