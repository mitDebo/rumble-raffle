using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using RumbleRaffle.Core.Auth;
using RumbleRaffle.Core.Database;
using RumbleRaffle.Core.Storage;

namespace RumbleRaffle.Core;

public static class ServiceCollectionExtensions
{
    // Registers everything Core owns. Api's Program.cs calls this instead
    // of knowing about EF Core, Npgsql, Supabase Storage's HTTP API, or JWT
    // validation directly, keeping the composition root thin. The
    // connection string, the storage client's base address/auth header,
    // and the JWT issuer/JWKS URL are all resolved lazily (via the
    // IServiceProvider/IConfiguration passed into each callback here)
    // rather than read eagerly at registration time -- see
    // ConnectionStrings.Resolve's callers in Program.cs for why that
    // matters for tests.
    public static IServiceCollection AddRumbleRaffleCore(this IServiceCollection services)
    {
        services.AddDbContext<RumbleRaffleDbContext>((sp, options) =>
            options.UseNpgsql(ConnectionStrings.Resolve(sp.GetRequiredService<IConfiguration>()))
                .UseSnakeCaseNamingConvention());

        services.AddHttpClient<IImageStorage, SupabaseImageStorage>((sp, client) =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var baseUrl = configuration["Supabase:Url"]
                ?? throw new InvalidOperationException(
                    "Supabase:Url is not configured. Set Supabase__Url (backend/.env " +
                    "locally, a real environment variable in production).");
            // Trailing slash matters: HttpClient/Uri combine a relative
            // request URI onto BaseAddress by replacing everything after
            // the last "/", so a base address without one would silently
            // drop the entire host once a request path is added.
            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");

            // Deliberately not required yet -- no bucket/key exists until a
            // real upload is needed (see 5.2 in tasks.md). Left unset for
            // now, SupabaseImageStorage's own request will fail with a
            // clear 401 from Supabase the day something actually calls it
            // before this is configured, rather than blocking app startup
            // for a dependency nothing uses yet.
            var secretKey = configuration["Supabase:StorageSecretKey"];
            if (!string.IsNullOrEmpty(secretKey))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", secretKey);
            }
        });

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        // AddJwtBearer(Action<JwtBearerOptions>) only offers eager
        // configuration, which would read Supabase:Url before
        // WebApplicationFactory<Program> has spliced in its test
        // configuration (same problem ConnectionStrings.Resolve's callers
        // solve above) -- AddOptions(...).Configure<IConfiguration>(...) is
        // the supported way to get a lazy, DI-resolved options callback
        // instead.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IConfiguration>((options, configuration) =>
            {
                var baseUrl = configuration["Supabase:Url"]
                    ?? throw new InvalidOperationException(
                        "Supabase:Url is not configured. Set Supabase__Url (backend/.env " +
                        "locally, a real environment variable in production).");
                var issuer = $"{baseUrl.TrimEnd('/')}/auth/v1";

                // Supabase doesn't expose OIDC discovery yet, so this is
                // built by hand instead of just setting Authority and
                // letting JwtBearer fetch discovery itself -- see
                // JwksOnlyConfigurationRetriever for the full reasoning.
                // ConfigurationManager still gives the normal caching,
                // periodic refresh, and forced-refresh-on-unknown-kid
                // behavior for free.
                options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                    $"{issuer}/.well-known/jwks.json",
                    new JwksOnlyConfigurationRetriever(),
                    new HttpDocumentRetriever());

                // Keep claim types exactly as Supabase issues them (e.g.
                // "sub") instead of ASP.NET Core's legacy behavior of
                // remapping short JWT claim names to long XML/SOAP-style
                // claim type URIs.
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    // Supabase issues "authenticated" as the audience for
                    // any signed-in user, regardless of which provider (or
                    // magic link) they signed in with.
                    ValidateAudience = true,
                    ValidAudience = "authenticated",
                    ValidateLifetime = true,
                };
            });

        services.AddAuthorization();

        return services;
    }
}
