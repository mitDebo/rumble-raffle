# Rumble Raffle — Backend

ASP.NET Core Web API. Currently just a walking-skeleton milestone: one endpoint
(`GET /api/hello`, returns `hello, world`) plus placeholder unit and
integration test projects, proving out the build/test/deploy pipeline before
any real feature work starts.

No `.sln` file yet — commands below target each project directly. One will be
added once there's more than a couple of projects to justify it.

## Run it locally

```bash
dotnet run --project src/RumbleRaffle.Api
```

Then hit `http://localhost:<port>/api/hello` (the port is printed on startup).

## Run the tests

```bash
dotnet test tests/RumbleRaffle.Api.UnitTests
dotnet test tests/RumbleRaffle.Api.IntegrationTests
```

Both are placeholder `Assert.True(true)` tests for now — they exist to prove
the CI pipeline actually runs a unit-test step and an integration-test step,
not to cover real behavior yet.
