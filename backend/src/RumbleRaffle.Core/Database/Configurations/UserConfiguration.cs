using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RumbleRaffle.Core.Entities;

namespace RumbleRaffle.Core.Database.Configurations;

// Kept separate from the User class itself, which should just describe
// what a user is, not know anything about how it's persisted.
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        // Never database-generated -- always copied from Supabase Auth's
        // own auth.users.id by the trigger that creates this row in the
        // first place. Without this, EF Core would assume it owns id
        // generation and get it wrong.
        builder.Property(u => u.Id).ValueGeneratedNever();

        builder.Property(u => u.DisplayName).IsRequired();

        builder.Property(u => u.Email).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.Flags).HasDefaultValue(UserFlags.None);
        builder.Property(u => u.CreatedAt).HasDefaultValueSql("now()");
    }
}
