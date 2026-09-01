using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using RumbleRaffle.Core;

// Load backend/.env (one directory up from this project) for local dev.
// In production the container gets real environment variables from Docker
// Compose instead, so a missing .env file here is expected, not an error.
try
{
    DotNetEnv.Env.TraversePath().Load();
}
catch (FileNotFoundException)
{
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRumbleRaffleCore();

// Resolved lazily via IServiceProvider (same as AddRumbleRaffleCore's own
// DbContext registration) rather than read eagerly here, because
// WebApplicationFactory<Program> only splices its test-configured
// connection string in when builder.Build() runs.
builder.Services.AddHealthChecks()
    .AddNpgSql(
        sp => ConnectionStrings.Resolve(sp.GetRequiredService<IConfiguration>()),
        name: "postgres",
        tags: new[] { "ready" });

var app = builder.Build();

// Liveness: is the process itself responsive? Deliberately runs no checks
// — it should never depend on external services like the database,
// SignalR, or image storage, since restarting this container wouldn't fix
// an outage in any of those; it would just add a pointless restart on top.
// MapHealthChecks with no options runs every registered check by default,
// so this needs an explicit predicate that matches nothing — leaving it
// off only looked safe before anything was registered at all.
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => false,
});

// Readiness: can this instance actually serve traffic right now? As each
// dependency (database, SignalR, image storage) is added elsewhere in the
// app, register its check with the "ready" tag and it folds into this
// endpoint automatically — nothing here needs to change.
app.MapHealthChecks("/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
});

// Startup: has this instance finished its initial boot sequence? Same
// tag-based pattern as readiness, using "startup" instead.
app.MapHealthChecks("/startup", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("startup"),
});

app.Run();

// Exposed so WebApplicationFactory<Program> can be used from integration tests.
public partial class Program { }
