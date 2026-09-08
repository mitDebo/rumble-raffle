using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using RumbleRaffle.Core.Database;

namespace RumbleRaffle.Api.IntegrationTests.Scaffolding;

// PostgresApiFactory's container starts out completely empty -- fine for
// ReadyEndpointDatabaseTests, which only needs raw connectivity. Anything
// that actually reads or writes our real schema (public.users, the
// auth.users sync trigger from 1.7.1) needs this instead: it additionally
// stubs a minimal auth.users table -- Supabase Auth's real schema, which
// we don't own or migrate ourselves, just enough of its shape
// (id/email/raw_user_meta_data) to drive our own trigger -- then runs our
// own EF Core migrations for real, the same way `dotnet ef database
// update` would against the real Supabase database.
public sealed class MigratedPostgresApiFactory : PostgresApiFactory
{
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        await using (var connection = new NpgsqlConnection(ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE SCHEMA IF NOT EXISTS auth;
                CREATE TABLE IF NOT EXISTS auth.users (
                    id uuid PRIMARY KEY,
                    email text,
                    raw_user_meta_data jsonb NOT NULL DEFAULT '{}'::jsonb
                );
                """;
            await command.ExecuteNonQueryAsync();
        }

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RumbleRaffleDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
