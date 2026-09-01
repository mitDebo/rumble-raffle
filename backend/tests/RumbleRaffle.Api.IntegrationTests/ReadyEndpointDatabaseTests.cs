using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace RumbleRaffle.Api.IntegrationTests;

// Proves the Postgres check registered in Program.cs actually reaches a
// real database, unlike HealthEndpointsTests' /health and /startup cases
// (which have nothing registered to check at all). The database here is a
// throwaway Testcontainers-provisioned Postgres instance, not Supabase.
// The failure counterpart to this (database unreachable) lives in
// ReadyEndpointFailureTests, against a separate factory.
public class ReadyEndpointDatabaseTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public ReadyEndpointDatabaseTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Ready_ReturnsHealthyAndListsPostgresAsHealthy_WhenDatabaseIsReachable()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Healthy", body.GetProperty("status").GetString());

        var checks = body.GetProperty("checks").EnumerateArray().ToList();
        var postgres = Assert.Single(checks);
        Assert.Equal("postgres", postgres.GetProperty("name").GetString());
        Assert.Equal("Healthy", postgres.GetProperty("status").GetString());
    }
}
