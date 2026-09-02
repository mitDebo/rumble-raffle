namespace RumbleRaffle.Core.Entities;

// Global, account-level flags. Deliberately NOT where per-event roles like
// host/attendee live -- those stay purely relational (an event's HostId,
// an attendee/invite row), granting privileges only for that one event.
// This bitmask only ever holds things true about a person everywhere, and
// today that's just one: a real, enforced admin role for MVP (gating
// official-event and wrestler-roster management). Add the next flag as
// the next free bit -- 2, 4, 8, ... -- without a schema change.
[Flags]
public enum UserFlags
{
    None = 0,
    Admin = 1,
}

// Companion row to Supabase Auth's own auth.users table -- one per person,
// sharing that table's id rather than generating its own (a Postgres
// trigger creates this row the moment auth.users gets one). Only holds
// what Supabase's own table doesn't already give us, or what the app wants
// to own independently of whatever an OAuth provider originally supplied:
// DisplayName/AvatarUrl start as a one-time copy of the sign-in provider's
// data, but are the app's own from that point on (e.g. 5.2's upload flow
// overwrites AvatarUrl directly, with no need to touch Supabase's copy).
public class User
{
    public Guid Id { get; set; }

    public required string DisplayName { get; set; }

    public string? AvatarUrl { get; set; }

    // Synced copy of auth.users.email (same trigger), so invite-by-email
    // lookups (2.6/2.7) stay ordinary EF Core queries against this
    // DbContext rather than reaching into Supabase's internal auth schema.
    public required string Email { get; set; }

    public UserFlags Flags { get; set; } = UserFlags.None;

    public DateTimeOffset CreatedAt { get; set; }
}
