using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RumbleRaffle.Api.IntegrationTests.Scaffolding;
using Xunit;

namespace RumbleRaffle.Api.IntegrationTests;

public class HealthEndpointsTests : IClassFixture<NoDatabaseApiFactory>
{
    private readonly NoDatabaseApiFactory _factory;

    public HealthEndpointsTests(NoDatabaseApiFactory factory)
    {
        _factory = factory;
    }

    // /ready now has a real database check registered (see
    // ReadyEndpointDatabaseTests) — only /health and /startup still have
    // nothing registered, since neither has a dependency check yet
    // (liveness never will; startup will once something needs it).
    [Theory]
    [InlineData("/health")]
    [InlineData("/startup")]
    public async Task Endpoint_ReturnsHealthyWhenNoChecksAreRegisteredYet(string path)
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Healthy", body.GetProperty("status").GetString());
        // Neither endpoint has anything registered against it yet.
        Assert.Empty(body.GetProperty("checks").EnumerateArray());
    }
}
