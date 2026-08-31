using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();

var app = builder.Build();

// Liveness: is the process itself responsive? Deliberately has no checks —
// it should never depend on external services like the database, SignalR,
// or image storage, since restarting this container wouldn't fix an outage
// in any of those; it would just add a pointless restart on top.
app.MapHealthChecks("/health");

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
