using System.Net;
using Xunit;

namespace RumbleRaffle.Api.IntegrationTests;

// Proves the Postgres check registered in Program.cs actually reaches a
// real database, unlike HealthEndpointsTests' /health and /startup cases
// (which have nothing registered to check at all). The database here is a
// throwaway Testcontainers-provisioned Postgres instance, not Supabase.
public class ReadyEndpointDatabaseTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public ReadyEndpointDatabaseTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Ready_ReturnsHealthy_WhenDatabaseIsReachable()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal("Healthy", body);
    }
}
