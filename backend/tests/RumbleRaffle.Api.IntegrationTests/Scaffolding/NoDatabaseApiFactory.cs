using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace RumbleRaffle.Api.IntegrationTests.Scaffolding;

// /health and /startup never resolve a connection string at all (Program.cs
// only reads it lazily, when the "ready"-tagged check or the DbContext is
// actually used, and neither happens for those two endpoints). This
// placeholder exists as a safety net rather than a strict requirement — if
// something in this app ever starts resolving the connection string
// eagerly, HealthEndpointsTests should keep working without needing
// backend/.env or a CI secret.
public sealed class NoDatabaseApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] =
                    "Host=unused;Port=5432;Database=unused;Username=unused;Password=unused",
            });
        });
    }
}
