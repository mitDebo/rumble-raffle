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
            // own grants. The second block changes that default going
            // forward (for functions subsequently created by whichever
            // role runs this migration), so a future function added to
            // this schema doesn't silently repeat this same warning.
            //
            // anon/authenticated only exist on a real Supabase-managed
            // Postgres -- a vanilla instance (Testcontainers in the
            // integration tests, CI's scratch database) has neither, so
            // referencing them unconditionally would fail this migration
            // outright with "role does not exist" anywhere but Supabase.
            // Guarding each block on pg_roles keeps this migration doing
            // the same thing on Supabase while safely no-op'ing everywhere
            // else.
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'anon') THEN
                        REVOKE EXECUTE ON FUNCTION public.handle_new_auth_user() FROM anon;
                        ALTER DEFAULT PRIVILEGES IN SCHEMA public REVOKE EXECUTE ON FUNCTIONS FROM anon;
                    END IF;

                    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'authenticated') THEN
                        REVOKE EXECUTE ON FUNCTION public.handle_new_auth_user() FROM authenticated;
                        ALTER DEFAULT PRIVILEGES IN SCHEMA public REVOKE EXECUTE ON FUNCTIONS FROM authenticated;
                    END IF;
                END
                $$;

                REVOKE EXECUTE ON FUNCTION public.handle_new_auth_user() FROM PUBLIC;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                GRANT EXECUTE ON FUNCTION public.handle_new_auth_user() TO PUBLIC;

                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'anon') THEN
                        ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT EXECUTE ON FUNCTIONS TO anon;
                        GRANT EXECUTE ON FUNCTION public.handle_new_auth_user() TO anon;
                    END IF;

                    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'authenticated') THEN
                        ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT EXECUTE ON FUNCTIONS TO authenticated;
                        GRANT EXECUTE ON FUNCTION public.handle_new_auth_user() TO authenticated;
                    END IF;
                END
                $$;
                """);
        }
    }
}
