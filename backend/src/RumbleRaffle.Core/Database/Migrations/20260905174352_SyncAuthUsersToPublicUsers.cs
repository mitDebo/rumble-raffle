using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RumbleRaffle.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class SyncAuthUsersToPublicUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Hand-written raw SQL, not model-driven: auth.users is Supabase
            // Auth's own table, never part of our EF model, so this can't
            // be expressed as a normal CreateTable/AddColumn migration.
            // (See 5.6's note: this content is invisible to a future
            // migration squash and needs to be carried forward by hand.)
            //
            // handle_new_auth_user() is SECURITY DEFINER (running as this
            // function's owner, not the caller) because the actual INSERT
            // into auth.users happens under Supabase's internal auth
            // service role, which has no grants on our public schema --
            // this mirrors Supabase's own documented pattern for syncing a
            // public profile table from auth.users. SET search_path=public
            // pins the schema search path so the function can't be tricked
            // by a caller-controlled search_path (standard hardening for
            // SECURITY DEFINER functions).
            //
            // display_name falls back through full_name -> name -> the
            // email's local part, since OAuth providers vary in which
            // metadata key they populate and magic link supplies neither,
            // but display_name is a required column. avatar_url is left
            // NULL when absent. flags/created_at are left untouched so
            // UserConfiguration's column defaults (0 / now()) apply.
            migrationBuilder.Sql(
                """
                CREATE FUNCTION public.handle_new_auth_user()
                RETURNS trigger
                LANGUAGE plpgsql
                SECURITY DEFINER
                SET search_path = public
                AS $$
                BEGIN
                    INSERT INTO public.users (id, email, display_name, avatar_url)
                    VALUES (
                        NEW.id,
                        NEW.email,
                        COALESCE(
                            NEW.raw_user_meta_data ->> 'full_name',
                            NEW.raw_user_meta_data ->> 'name',
                            split_part(NEW.email, '@', 1)
                        ),
                        NEW.raw_user_meta_data ->> 'avatar_url'
                    )
                    ON CONFLICT (id) DO NOTHING;

                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER on_auth_user_created
                AFTER INSERT ON auth.users
                FOR EACH ROW
                EXECUTE FUNCTION public.handle_new_auth_user();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Only ever drops what Up() created -- never touches auth.users
            // itself, which isn't ours to manage.
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS on_auth_user_created ON auth.users;
                DROP FUNCTION IF EXISTS public.handle_new_auth_user();
                """);
        }
    }
}
