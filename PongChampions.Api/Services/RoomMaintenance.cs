using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PongChampions.Data;
using PongChampions.Data.Enums;

namespace PongChampions.Api.Services;

public sealed class RoomMaintenance(
    AppDbContext context,
    RoomCoordinator coordinator,
    IGameSessionService sessions,
    RoomEvents events,
    TimeProvider clock,
    IOptions<GameOptions> options)
{
    public async Task CleanAsync(bool recoverAfterRestart, CancellationToken token)
    {
        var closed = new List<string>();

        await using (await coordinator.BeginAsync(context, token))
        {
            var cutoff = clock.GetUtcNow().UtcDateTime.AddMinutes(-options.Value.LobbyLifetimeMinutes);
            var abandoned = sessions.GetAbandonedRooms();

            var rooms = await context.Rooms
                .Where(r => r.Status == RoomStatus.InGame ||
                    ((r.Status == RoomStatus.Ready || r.Status == RoomStatus.WaitingForPlayers) &&
                    r.CreatedAt < cutoff))
                .ToListAsync(token);

            foreach (var room in rooms)
            {
                if (room.Status == RoomStatus.InGame && !recoverAfterRestart && !abandoned.Contains(room.Code))
                    continue;

                room.Status = RoomStatus.Cancelled;
                closed.Add(room.Code);
            }

            await context.SaveChangesAsync(token);
            await context.Database.CurrentTransaction!.CommitAsync(token);

            foreach (var code in closed)
                sessions.RemoveSession(code);
        }

        foreach (var code in closed)
            await events.SendAsync(code, "RoomClosed", new { code, status = RoomStatus.Cancelled.ToString() }, token);
    }
}
