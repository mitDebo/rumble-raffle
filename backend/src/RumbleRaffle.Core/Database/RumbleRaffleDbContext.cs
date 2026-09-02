using Microsoft.EntityFrameworkCore;
using RumbleRaffle.Core.Database.Configurations;
using RumbleRaffle.Core.Entities;

namespace RumbleRaffle.Core.Database;

public class RumbleRaffleDbContext : DbContext
{
    public RumbleRaffleDbContext(DbContextOptions<RumbleRaffleDbContext> options)
        : base(options)
    {
    }

    // "users", not "Users" -- ServiceCollectionExtensions.AddRumbleRaffleCore
    // configures snake_case table/column naming (via EFCore.NamingConventions)
    // for every entity from here on, matching Postgres/Supabase's own
    // convention (auth.users itself uses e.g. created_at, not CreatedAt).
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
    }
}
