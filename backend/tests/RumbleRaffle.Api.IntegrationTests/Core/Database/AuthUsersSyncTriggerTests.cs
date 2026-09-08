using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using RumbleRaffle.Api.IntegrationTests.Scaffolding;
using RumbleRaffle.Core.Database;
using Xunit;

namespace RumbleRaffle.Api.IntegrationTests.Core.Database;

// Proves the auth.users -> public.users sync trigger (1.7.1) actually
// fires -- without it, every sign-in method in 1.7 would leave
// /api/users/me 404ing forever, since Supabase populates auth.users on
// sign-in but nothing would populate our own public.users row. This is a
// pure database test: it inserts directly into MigratedPostgresApiFactory's
// stubbed auth.users table (standing in for what Supabase's own auth
// service does server-side on a real sign-in) and reads back through
// RumbleRaffleDbContext, with no HTTP endpoint involved.
public class AuthUsersSyncTriggerTests : IClassFixture<MigratedPostgresApiFactory>
{
    private readonly MigratedPostgresApiFactory _factory;

    public AuthUsersSyncTriggerTests(MigratedPostgresApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task InsertingIntoAuthUsers_CreatesMatchingPublicUsersRow()
    {
        var id = Guid.NewGuid();

        await using (var connection = new NpgsqlConnection(_factory.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO auth.users (id, email, raw_user_meta_data)
                VALUES (@id, @email, @metadata::jsonb);
                """;
            command.Parameters.AddWithValue("id", id);
            command.Parameters.AddWithValue("email", "kelly@example.com");
            command.Parameters.AddWithValue(
                "metadata",
                """{"full_name": "Kelly Weaver", "avatar_url": "https://example.com/avatar.png"}""");
            await command.ExecuteNonQueryAsync();
        }

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RumbleRaffleDbContext>();
        var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Id == id);

        Assert.NotNull(user);
        Assert.Equal("kelly@example.com", user!.Email);
        Assert.Equal("Kelly Weaver", user.DisplayName);
        Assert.Equal("https://example.com/avatar.png", user.AvatarUrl);
    }

    [Fact]
    public async Task InsertingIntoAuthUsers_WithNoNameMetadata_FallsBackDisplayNameToEmailLocalPart()
    {
        // Magic link sign-ins (no OAuth profile) leave raw_user_meta_data
        // empty -- display_name is a required column, so the trigger needs
        // a sane fallback rather than failing the insert outright.
        var id = Guid.NewGuid();

        await using (var connection = new NpgsqlConnection(_factory.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO auth.users (id, email) VALUES (@id, @email);";
            command.Parameters.AddWithValue("id", id);
            command.Parameters.AddWithValue("email", "no-name@example.com");
            await command.ExecuteNonQueryAsync();
        }

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RumbleRaffleDbContext>();
        var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Id == id);

        Assert.NotNull(user);
        Assert.Equal("no-name", user!.DisplayName);
        Assert.Null(user.AvatarUrl);
    }
}
