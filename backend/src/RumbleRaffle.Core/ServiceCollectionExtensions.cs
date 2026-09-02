using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RumbleRaffle.Core.Database;
using RumbleRaffle.Core.Storage;

namespace RumbleRaffle.Core;

public static class ServiceCollectionExtensions
{
    // Registers everything Core owns. Api's Program.cs calls this instead
    // of knowing about EF Core, Npgsql, or Supabase Storage's HTTP API
    // directly, keeping the composition root thin. The connection string
    // and the storage client's base address/auth header are all resolved
    // lazily (via the IServiceProvider passed into each callback here)
    // rather than read eagerly at registration time — see
    // ConnectionStrings.Resolve's callers in Program.cs for why that
    // matters for tests.
    public static IServiceCollection AddRumbleRaffleCore(this IServiceCollection services)
    {
        services.AddDbContext<RumbleRaffleDbContext>((sp, options) =>
            options.UseNpgsql(ConnectionStrings.Resolve(sp.GetRequiredService<IConfiguration>())));

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

            // Deliberately not required yet — no bucket/key exists until a
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

        return services;
    }
}
