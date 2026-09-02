using System.Net;
using RumbleRaffle.Api.IntegrationTests.Scaffolding;
using Xunit;

namespace RumbleRaffle.Api.IntegrationTests.Endpoints;

public class MeEndpointTests : IClassFixture<NoDatabaseApiFactory>
{
    private readonly NoDatabaseApiFactory _factory;

    public MeEndpointTests(NoDatabaseApiFactory factory)
    {
        _factory = factory;
    }

    // Only the reject path is proven here -- confirming a real Supabase JWT
    // is accepted (the success path) happens in 1.7, once a real sign-in
    // flow exists to produce one to test with. NoDatabaseApiFactory is
    // safe for this even though it has no real database or Supabase
    // project behind it: an anonymous request never reaches the DbContext
    // or the JWKS ConfigurationManager -- JwtBearerHandler only fetches
    // configuration once an actual bearer token needs validating, and
    // RequireAuthorization() rejects this request before GetMe ever runs.
    [Fact]
    public async Task Me_WithNoAuthorizationHeader_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
