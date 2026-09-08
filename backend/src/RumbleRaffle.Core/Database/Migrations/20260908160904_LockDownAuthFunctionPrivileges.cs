using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RumbleRaffle.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class LockDownAuthFunctionPrivileges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // RevokeExecuteOnAuthTriggerFunction only revoked from PUBLIC,
            // which didn't clear Supabase's advisor warning -- anon and
            // authenticated turned out to have EXECUTE granted directly,
            // not inherited through PUBLIC. Supabase sets a default-
            // privileges rule on the public schema so every new function
            // created there automatically grants EXECUTE to
            // anon/authenticated (and service_role) regardless of PUBLIC's
            // own grants. The second statement changes that default going
            // forward (for functions subsequently created by whichever
            // role runs this migration), so a future function added to
            // this schema doesn't silently repeat this same warning.
            migrationBuilder.Sql(
                """
                REVOKE EXECUTE ON FUNCTION public.handle_new_auth_user() FROM PUBLIC, anon, authenticated;
                ALTER DEFAULT PRIVILEGES IN SCHEMA public REVOKE EXECUTE ON FUNCTIONS FROM anon, authenticated;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT EXECUTE ON FUNCTIONS TO anon, authenticated;
                GRANT EXECUTE ON FUNCTION public.handle_new_auth_user() TO PUBLIC, anon, authenticated;
                """);
        }
    }
}
