using Microsoft.Extensions.Configuration;
using Npgsql;

namespace RumbleRaffle.Core.Database;

public static class ConnectionStrings
{
    // Reads ConnectionStrings:Default directly via the indexer rather than
    // the GetConnectionString("Default") extension (which lives in a
    // separate, less minimal package) — for a colon-separated key like
    // this, they're equivalent.
    public static string Resolve(IConfiguration configuration)
    {
        var raw = configuration["ConnectionStrings:Default"]
            ?? throw new InvalidOperationException(
                "ConnectionStrings:Default is not configured. Set ConnectionStrings__Default " +
                "(backend/.env locally, a real environment variable in production).");
        return Normalize(raw);
    }

    // Supabase's dashboard hands out connection strings as a "postgresql://"
    // URI, but Npgsql's connection string parser expects the ADO.NET
    // keyword=value format (Host=...;Port=...;...). Convert when needed so
    // either format works — Testcontainers, for example, already produces
    // the keyword=value format, so this is a no-op for the integration
    // tests.
    public static string Normalize(string raw)
    {
        if (!raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return raw;
        }

        var uri = new Uri(raw);
        var userInfo = uri.UserInfo.Split(':', 2);

        var connectionStringBuilder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
            Database = uri.AbsolutePath.TrimStart('/'),
        };

        return connectionStringBuilder.ConnectionString;
    }
}
