using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using RumbleRaffle.Api;

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

var rawConnectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:Default is not configured. Set ConnectionStrings__Default " +
        "(backend/.env locally, a real environment variable in production).");
var connectionString = NormalizePostgresConnectionString(rawConnectionString);

builder.Services.AddDbContext<RumbleRaffleDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres", tags: new[] { "ready" });

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

// Supabase's dashboard hands out connection strings as a "postgresql://"
// URI, but Npgsql's connection string parser expects the ADO.NET
// keyword=value format (Host=...;Port=...;...). Convert when needed so
// either format works — Testcontainers, for example, already produces the
// keyword=value format, so this is a no-op for the integration tests.
static string NormalizePostgresConnectionString(string raw)
{
    if (!raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
        !raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        return raw;
    }

    var uri = new Uri(raw);
    var userInfo = uri.UserInfo.Split(':', 2);

    var connectionStringBuilder = new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port > 0 ? uri.Port : 5432,
        Username = Uri.UnescapeDataString(userInfo[0]),
        Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
        Database = uri.AbsolutePath.TrimStart('/'),
    };

    return connectionStringBuilder.ConnectionString;
}

// Exposed so WebApplicationFactory<Program> can be used from integration tests.
public partial class Program { }
