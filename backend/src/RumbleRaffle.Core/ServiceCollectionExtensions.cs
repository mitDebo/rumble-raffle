using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace RumbleRaffle.Core;

public static class ServiceCollectionExtensions
{
    // Registers everything Core owns. Api's Program.cs calls this instead
    // of knowing about EF Core or Npgsql directly, keeping the composition
    // root thin. The connection string is resolved lazily (per DbContext
    // instance, via the IServiceProvider passed in here) rather than read
    // eagerly at registration time — see ConnectionStrings.Resolve's
    // callers in Program.cs for why that matters for tests.
    public static IServiceCollection AddRumbleRaffleCore(this IServiceCollection services)
    {
        services.AddDbContext<RumbleRaffleDbContext>((sp, options) =>
            options.UseNpgsql(ConnectionStrings.Resolve(sp.GetRequiredService<IConfiguration>())));

        return services;
    }
}
