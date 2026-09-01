using Microsoft.EntityFrameworkCore;

namespace RumbleRaffle.Core;

// Intentionally empty for now — task 1.5 only needs to prove EF Core can
// connect to Postgres and run a migration end-to-end. Real DbSets land with
// the entities that need them in later Phase 1/2 tasks.
public class RumbleRaffleDbContext : DbContext
{
    public RumbleRaffleDbContext(DbContextOptions<RumbleRaffleDbContext> options)
        : base(options)
    {
    }
}
