using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RumbleRaffle.Api.IntegrationTests.Scaffolding;
using Xunit;

namespace RumbleRaffle.Api.IntegrationTests.HealthChecks;

// The failure counterpart to ReadyEndpointDatabaseTests: proves /ready
// reports 503 and names the failing check when a dependency isn't
// reachable, rather than just an opaque "Unhealthy".
public class ReadyEndpointFailureTests : IClassFixture<UnreachableDatabaseApiFactory>
{
    private readonly UnreachableDatabaseApiFactory _factory;

    public ReadyEndpointFailureTests(UnreachableDatabaseApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Ready_ReturnsServiceUnavailableAndListsPostgresAsUnhealthy_WhenDatabaseIsUnreachable()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Unhealthy", body.GetProperty("status").GetString());

        var checks = body.GetProperty("checks").EnumerateArray().ToList();
        var postgres = Assert.Single(checks);
        Assert.Equal("postgres", postgres.GetProperty("name").GetString());
        Assert.Equal("Unhealthy", postgres.GetProperty("status").GetString());
    }
}
