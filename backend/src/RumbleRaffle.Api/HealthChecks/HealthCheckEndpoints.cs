using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace RumbleRaffle.Api.HealthChecks;

// How the three health checks get exposed over HTTP. What gets checked is
// a separate concern, decided in Program.cs (AddHealthChecks().AddNpgSql(),
// and whatever SignalR/storage checks join it later) -- this only maps the
// endpoints and controls how a check's result gets serialized.
public static class HealthCheckEndpoints
{
    public static void MapRumbleRaffleHealthChecks(this WebApplication app)
    {
        // Liveness: is the process itself responsive? Deliberately runs no
        // checks -- it should never depend on external services like the
        // database, SignalR, or image storage, since restarting this
        // container wouldn't fix an outage in any of those; it would just
        // add a pointless restart on top. MapHealthChecks with no options
        // runs every registered check by default, so this needs an
        // explicit predicate that matches nothing -- leaving it off only
        // looked safe before anything was registered at all.
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = WriteHealthCheckResponse,
        });

        // Readiness: can this instance actually serve traffic right now?
        // As each dependency (database, SignalR, image storage) is added
        // elsewhere in the app, register its check with the "ready" tag
        // and it folds into this endpoint automatically -- nothing here
        // needs to change.
        app.MapHealthChecks("/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = WriteHealthCheckResponse,
        });

        // Startup: has this instance finished its initial boot sequence?
        // Same tag-based pattern as readiness, using "startup" instead.
        app.MapHealthChecks("/startup", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("startup"),
            ResponseWriter = WriteHealthCheckResponse,
        });
    }

    // Lists each registered check by name and status, instead of just the
    // aggregate "Healthy"/"Unhealthy" string ASP.NET Core's default writer
    // produces -- so a failure names which dependency is down, not just
    // that something is. The overall HTTP status code (200 for
    // Healthy/Degraded, 503 for Unhealthy) is unaffected -- that's
    // HealthCheckOptions' ResultStatusCodes default, already correct,
    // nothing to change there. Deliberately omits each check's
    // Description/Exception: those can carry details (e.g. connection info
    // in a Postgres exception message) that shouldn't be exposed over an
    // unauthenticated endpoint. Anything more detailed than pass/fail per
    // service belongs in server-side logs, not this response body.
    private static Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var payload = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
            }),
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
