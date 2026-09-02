using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace RumbleRaffle.Core.Auth;

// ASP.NET Core's JwtBearer handler expects a full OIDC discovery document
// (the JSON payload normally found at /.well-known/openid-configuration)
// to auto-configure signing keys via JwtBearerOptions.Authority. Supabase
// doesn't expose one yet -- their own team has confirmed full OIDC
// discovery support is still in progress, not shipped -- only the bare
// JWKS document itself, at /auth/v1/.well-known/jwks.json. This retriever
// bridges that gap: it fetches the bare JWKS and wraps it in the
// OpenIdConnectConfiguration shape the handler actually consumes, so
// JwtBearerOptions.ConfigurationManager still gets its normal behavior for
// free -- caching, periodic refresh, and (notably) an automatic forced
// refresh-and-retry if a token's "kid" isn't found in the cached keys,
// which matters if Supabase ever rotates its signing keys. Delete this,
// and go back to plain Authority-based configuration, once Supabase ships
// real OIDC discovery.
public class JwksOnlyConfigurationRetriever : IConfigurationRetriever<OpenIdConnectConfiguration>
{
    public async Task<OpenIdConnectConfiguration> GetConfigurationAsync(
        string address, IDocumentRetriever retriever, CancellationToken cancel)
    {
        var json = await retriever.GetDocumentAsync(address, cancel);

        var configuration = new OpenIdConnectConfiguration();
        var jwks = new JsonWebKeySet(json);
        foreach (var signingKey in jwks.GetSigningKeys())
        {
            configuration.SigningKeys.Add(signingKey);
        }

        return configuration;
    }
}
