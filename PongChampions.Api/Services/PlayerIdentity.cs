using Microsoft.EntityFrameworkCore;
using PongChampions.Api.Common;
using PongChampions.Data;
using PongChampions.Data.Entities;
using System.Security.Claims;

namespace PongChampions.Api.Services;

public static class PlayerIdentity
{
    public static Guid RegisteredUserId(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true || user.FindFirstValue("identity_kind") == "guest")
            throw new ApiException(401, "A registered account is required.");

        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            throw new ApiException(401, "A registered account is required.");

        return userId;
    }

    public static async Task<Guid?> ResolveAsync(
        ClaimsPrincipal user,
        Room room,
        AppDbContext context)
    {
        if (user.Identity?.IsAuthenticated != true)
            return null;

        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
            throw ApiException.Forbidden("Invalid player identity.");

        if (user.FindFirstValue("identity_kind") == "guest")
        {
            if (user.FindFirstValue("room_code") != room.Code || room.GuestPlayerId != id)
                throw ApiException.Forbidden("Guest credential is not valid for this room.");

            return id;
        }

        return await context.Players
            .Where(p => p.UserId == id)
            .Select(p => (Guid?)p.Id)
            .SingleOrDefaultAsync();
    }
}
