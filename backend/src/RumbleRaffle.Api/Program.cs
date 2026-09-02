using RumbleRaffle.Api.HealthChecks;
using RumbleRaffle.Api.Hubs;
using RumbleRaffle.Core;
using RumbleRaffle.Core.Database;

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
builder.Services.AddSignalR();

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

app.MapRumbleRaffleHealthChecks();

// Routed under /api to match the prefix nginx (production) and Vite's dev
// proxy (local) both use to route requests here in the first place -- see
// PingHubTests for the reasoning. Ping is 1.8's connectivity-only hub;
// real domain hubs will map alongside it here as they're built.
app.MapHub<PingHub>("/api/hubs/ping");

app.Run();

// Exposed so WebApplicationFactory<Program> can be used from integration tests.
public partial class Program { }
