using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using Xunit;

namespace RumbleRaffle.Api.IntegrationTests.Scaffolding;

// Spins up a real, throwaway Postgres container for tests that need to
// prove actual database connectivity (as opposed to HealthEndpointsTests'
// default factory, which never configures a reachable database at all).
// One container is shared across every test in a collection using this
// fixture and torn down once, after the last test runs — never against the
// real Supabase project, and nothing persists between test runs.
public sealed class PostgresApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("rumbleraffle_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Placeholder so JwtBearerOptions' lazy Configure<IConfiguration>
                // callback (ServiceCollectionExtensions.AddRumbleRaffleCore) has
                // something to read -- UseAuthentication() runs on every request,
                // even ones that never hit a protected endpoint, and would throw
                // on a real request otherwise. Never actually fetched: an
                // anonymous request never reaches JwtBearerHandler's configuration
                // manager, which only activates once a bearer token needs
                // validating.
                ["Supabase:Url"] = "https://fake.supabase.co",
                ["ConnectionStrings:Default"] = _dbContainer.GetConnectionString(),
            });
        });
    }

    public Task InitializeAsync() => _dbContainer.StartAsync();

    public new async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}
