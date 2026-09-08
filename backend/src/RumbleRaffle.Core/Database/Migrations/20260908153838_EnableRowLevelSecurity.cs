using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RumbleRaffle.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class EnableRowLevelSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Supabase's PostgREST layer auto-exposes every table in the
            // public schema over its REST API unless RLS is enabled --
            // flagged by Supabase's own security advisor for both tables
            // below. No policies are added: RLS with zero policies denies
            // all access by default to the anon/authenticated roles
            // PostgREST uses, which is exactly what we want -- neither
            // table is meant to be reachable that way. Our own backend is
            // unaffected: it connects as the postgres role directly (not
            // through PostgREST), which bypasses RLS by default. If the
            // frontend ever needs to query public.users directly through
            // Supabase instead of through our own /api/users/me, real
            // policies would need to be added at that point.
            //
            // __EFMigrationsHistory is EF Core's own internal bookkeeping
            // table, not part of our model -- same reasoning as
            // SyncAuthUsersToPublicUsers's raw SQL, this needs to be
            // carried forward by hand during a future migration squash
            // (see 5.6's note).
            migrationBuilder.Sql(
                """
                ALTER TABLE public.users ENABLE ROW LEVEL SECURITY;
                ALTER TABLE public."__EFMigrationsHistory" ENABLE ROW LEVEL SECURITY;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE public.users DISABLE ROW LEVEL SECURITY;
                ALTER TABLE public."__EFMigrationsHistory" DISABLE ROW LEVEL SECURITY;
                """);
        }
    }
}
