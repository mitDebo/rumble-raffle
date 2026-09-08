using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RumbleRaffle.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class RevokeExecuteOnAuthTriggerFunction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Postgres grants EXECUTE on a newly created function to PUBLIC
            // by default, which includes the anon/authenticated roles
            // PostgREST uses -- combined with handle_new_auth_user() being
            // SECURITY DEFINER, Supabase's advisor flags this as a
            // SECURITY DEFINER function callable over its auto-generated
            // RPC endpoint. In practice this function can't actually be
            // invoked that way (it's declared RETURNS trigger, and Postgres
            // only allows trigger functions to be called by the trigger
            // mechanism itself, not directly), and revoking EXECUTE doesn't
            // affect that -- trigger firing never checks the invoking
            // role's EXECUTE privilege on the function. Revoked anyway
            // since there's no cost to it and it clears the warning rather
            // than relying on that engine behavior remaining true.
            migrationBuilder.Sql(
                "REVOKE EXECUTE ON FUNCTION public.handle_new_auth_user() FROM PUBLIC;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "GRANT EXECUTE ON FUNCTION public.handle_new_auth_user() TO PUBLIC;");
        }
    }
}
