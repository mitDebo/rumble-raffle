using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace RumbleRaffle.Api.IntegrationTests;

// Program.cs now requires ConnectionStrings:Default just to start up, even
// though /health and /startup never touch the database. This factory
// supplies a placeholder value so HealthEndpointsTests never depends on
// backend/.env being present locally or a real secret being configured in
// CI — the value is never actually connected to.
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
