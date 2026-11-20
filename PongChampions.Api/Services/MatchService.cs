using Microsoft.EntityFrameworkCore;
using PongChampions.Api.Common;
using PongChampions.Api.Common.Dtos.Game;
using PongChampions.Data;
using PongChampions.Data.Entities;
using PongChampions.Data.Enums;

namespace PongChampions.Api.Services;

public class MatchService(
    AppDbContext context,
    RoomCoordinator coordinator,
    TimeProvider clock) : IMatchService
{
    public async Task<MatchCompletionResult> FinishMatchAsync(
        GameStateDto gameState,
        CancellationToken cancellationToken = default)
    {
        if (gameState.IsRunning || gameState.TargetScore <= 0 || gameState.HostScore < 0 ||
            gameState.GuestScore < 0 ||
            Math.Max(gameState.HostScore, gameState.GuestScore) != gameState.TargetScore ||
            gameState.HostScore == gameState.GuestScore)
            throw ApiException.Invalid("Invalid final game state.");

        await using var lease = await coordinator.BeginAsync(context, cancellationToken);

        var room = await context.Rooms
            .SingleOrDefaultAsync(
                r => r.Code == gameState.RoomCode,
                cancellationToken)
            ?? throw ApiException.NotFound("Room not found.");

        if (room.Status == RoomStatus.Cancelled)
            return MatchCompletionResult.Cancelled;

        if (room.Status == RoomStatus.Finished)
        {
            var matchExists = await context.Matches
                .AnyAsync(m => m.RoomId == room.Id, cancellationToken);

            if (!matchExists)
                throw ApiException.Conflict("Finished room has no result.");

            return MatchCompletionResult.AlreadyCompleted;
        }

        if (room.Status != RoomStatus.InGame)
            throw ApiException.Conflict("Room is not in game.");

        if (room.GuestPlayerId is null)
            throw ApiException.Conflict("Room has no guest.");

        if (room.MaxScore != gameState.TargetScore)
            throw ApiException.Conflict("Target score mismatch.");

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

            FinishedAt = clock.GetUtcNow().UtcDateTime
        };

        context.Matches.Add(match);

        room.Status = RoomStatus.Finished;

        await context.SaveChangesAsync(cancellationToken);
        await context.Database.CurrentTransaction!.CommitAsync(cancellationToken);

        return MatchCompletionResult.Created;
    }
}
