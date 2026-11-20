using Microsoft.EntityFrameworkCore;
using PongChampions.Api.Common.Dtos.Game;
using PongChampions.Data;
using PongChampions.Data.Entities;
using PongChampions.Data.Enums;

namespace PongChampions.Api.Services;

public class MatchService(AppDbContext context) : IMatchService
{
    public async Task<bool> FinishMatchAsync(
        GameStateDto gameState,
        CancellationToken cancellationToken = default)
    {
        if (gameState.IsRunning)
            throw new InvalidOperationException("Match is still running.");

        if (gameState.HostScore < gameState.TargetScore &&
            gameState.GuestScore < gameState.TargetScore)
            throw new InvalidOperationException("Target score has not been reached.");

        if (gameState.HostScore == gameState.GuestScore)
            throw new InvalidOperationException("Match cannot finish in a draw.");

        var room = await context.Rooms
            .SingleOrDefaultAsync(
                r => r.Code == gameState.RoomCode,
                cancellationToken)
            ?? throw new InvalidOperationException("Room not found.");

        if (room.Status == RoomStatus.Finished)
            return false;

        if (room.Status != RoomStatus.InGame)
            throw new InvalidOperationException("Room is not in game.");

        if (room.GuestPlayerId is null)
            throw new InvalidOperationException("Room has no guest.");

        if (room.MaxScore != gameState.TargetScore)
            throw new InvalidOperationException("Target score mismatch.");

        var match = new Match
        {
            RoomId = room.Id,
            Player1Id = room.HostPlayerId,
            Player2Id = room.GuestPlayerId.Value,

            Player1Score = gameState.HostScore,
            Player2Score = gameState.GuestScore,

            WinnerPlayerId = gameState.HostScore > gameState.GuestScore
                ? room.HostPlayerId
                : room.GuestPlayerId.Value,

            FinishedAt = DateTime.UtcNow
        };

        context.Matches.Add(match);

        room.Status = RoomStatus.Finished;

        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}