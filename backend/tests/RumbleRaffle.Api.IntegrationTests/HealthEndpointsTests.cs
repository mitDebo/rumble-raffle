using System.Net;
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
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal("Healthy", body);
    }
}
