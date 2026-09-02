using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using RumbleRaffle.Core.Database;

namespace RumbleRaffle.Api.Endpoints;

public static class UserEndpoints
{
    public static void MapRumbleRaffleUserEndpoints(this WebApplication app)
    {
        // The only endpoint for 1.6 -- proves JWT validation actually gates
        // something. RequireAuthorization() means an unauthenticated
        // request (no/invalid/expired bearer token) never reaches GetMe at
        // all; ASP.NET Core's auth middleware returns 401 on its own. 1.7
        // is what actually produces a real Supabase-issued token to prove
        // the accept path with -- this task only proves the reject path.
        app.MapGet("/api/users/me", GetMe).RequireAuthorization();
    }

    private static async Task<IResult> GetMe(ClaimsPrincipal principal, RumbleRaffleDbContext db)
    {
        // Defensive, not expected in practice: RequireAuthorization()
        // already guarantees a validated token by this point, and Supabase
        // always includes "sub". Guards against a malformed-but-otherwise-
        // valid token rather than trusting a null forever.
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (sub is null || !Guid.TryParse(sub, out var userId))
        {
            return Results.Unauthorized();
        }

        var user = await db.Users
            .Where(u => u.Id == userId)
            .Select(u => new { u.Id, u.DisplayName, u.AvatarUrl, u.Flags })
            .SingleOrDefaultAsync();

        // A valid Supabase token whose user row hasn't landed yet (the
        // auth.users -> public.users trigger from the schema discussion
        // hasn't been built as of this task) or was somehow removed.
        return user is null ? Results.NotFound() : Results.Ok(user);
    }
}
