using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace RumbleRaffle.Api.IntegrationTests;

// Points at a connection nothing is listening on, so the "ready"-tagged
// Postgres check fails fast with connection-refused rather than a real
// database going away mid-test (which PostgresApiFactory's shared,
// class-level container isn't set up to simulate without affecting its
// other tests). A short Timeout keeps this from ever hanging even if
// something is listening on this port in a given CI/dev environment.
public sealed class UnreachableDatabaseApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] =
                    "Host=127.0.0.1;Port=1;Database=unused;Username=unused;Password=unused;Timeout=2",
            });
        });
    }
}
