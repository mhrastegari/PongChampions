using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PongChampions.Api.Common.Dtos.Game;
using PongChampions.Api.Services;
using PongChampions.Data;
using PongChampions.Data.Entities;
using PongChampions.Data.Enums;

namespace PongChampions.Tests;

[Collection("backend")]
public sealed class PersistenceTests(BackendFixture fixture)
{
    private async Task<Room> SeedAsync(RoomStatus status = RoomStatus.InGame, DateTime? createdAt = null)
    {
        await using var db = fixture.NewContext();

        var room = new Room
        {
            Code = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant(),
            HostPlayer = new Player
            {
                DisplayName = "Host"
            },
            GuestPlayer = new Player
            {
                DisplayName = "Guest"
            },
            Status = status,
            CreatedAt = createdAt ?? DateTime.UtcNow
        };

        db.Rooms.Add(room);
        await db.SaveChangesAsync();

        return room;
    }

    private static GameStateDto Final(Room room)
    {
        return new()
        {
            RoomCode = room.Code,
            TargetScore = room.MaxScore,
            HostScore = room.MaxScore,
            GuestScore = 2,
            IsRunning = false
        };
    }

    [Fact]
    public async Task ConcurrentCompletionCreatesExactlyOneMatchAndUpdatesRoomAtomically()
    {
        var room = await SeedAsync();

        async Task<MatchCompletionResult> Finish()
        {
            await using var db = fixture.NewContext();

            // Independent coordinators force the test to exercise PostgreSQL's lock.
            return await new MatchService(db, new RoomCoordinator(), TimeProvider.System).FinishMatchAsync(Final(room));
        }

        var results = await Task.WhenAll(Enumerable.Range(0, 4)
            .Select(_ => Finish()));

        Assert.Single(results, r => r == MatchCompletionResult.Created);
        Assert.Equal(3, results.Count(r => r == MatchCompletionResult.AlreadyCompleted));

        await using var check = fixture.NewContext();
        var match = Assert.Single(await check.Matches
            .Where(m => m.RoomId == room.Id)
            .ToListAsync());

        Assert.Equal(room.HostPlayerId, match.WinnerPlayerId);
        Assert.Equal(room.MaxScore, match.Player1Score);
        Assert.Equal(2, match.Player2Score);
        Assert.NotNull(match.FinishedAt);
        Assert.Equal(RoomStatus.Finished, (await check.Rooms
            .SingleAsync(r => r.Id == room.Id)).Status);
    }

    [Fact]
    public async Task FailedSaveRollsBackRoomAndResultAndCanBeRetried()
    {
        var room = await SeedAsync();

        await using (var failing = fixture.NewContext(new FailSave()))
            await Assert.ThrowsAsync<IOException>(() => new MatchService(failing, new RoomCoordinator(), TimeProvider.System).FinishMatchAsync(Final(room)));

        await using (var check = fixture.NewContext())
        {
            Assert.Equal(RoomStatus.InGame, (await check.Rooms
                .SingleAsync(r => r.Id == room.Id)).Status);
            Assert.False(await check.Matches
                .AnyAsync(m => m.RoomId == room.Id));
        }

        await using var retry = fixture.NewContext();

        Assert.Equal(
            MatchCompletionResult.Created,
            await new MatchService(retry, new RoomCoordinator(), TimeProvider.System).FinishMatchAsync(Final(room)));
    }

    [Fact]
    public async Task CancelledRoomNeverCreatesFabricatedMatch()
    {
        var room = await SeedAsync(RoomStatus.Cancelled);
        await using var db = fixture.NewContext();

        Assert.Equal(
            MatchCompletionResult.Cancelled,
            await new MatchService(db, new RoomCoordinator(), TimeProvider.System).FinishMatchAsync(Final(room)));
        Assert.False(await db.Matches
            .AnyAsync(m => m.RoomId == room.Id));
    }

    [Fact]
    public async Task MaintenanceCancelsAbandonedGamesAndExpiredLobbiesWithoutResults()
    {
        var clock = new TestClock();
        var sessions = new GameSessionService(clock, Options.Create(new GameOptions()));
        var room = await SeedAsync();
        var lobby = await SeedAsync(RoomStatus.WaitingForPlayers, DateTime.UtcNow.AddHours(-1));

        sessions.CreateSession(room.Code, room.MaxScore);
        clock.Advance(TimeSpan.FromSeconds(61));

        await using var db = fixture.NewContext();

        var maintenance = new RoomMaintenance(
            db,
            new RoomCoordinator(),
            sessions,
            fixture.Factory.Services
                .GetRequiredService<RoomEvents>(),
            clock,
            Options.Create(new GameOptions()));

        await maintenance.CleanAsync(false, default);
        Assert.Null(sessions.GetSession(room.Code));
        Assert.Equal(RoomStatus.Cancelled, (await db.Rooms
            .SingleAsync(r => r.Id == room.Id)).Status);
        Assert.Equal(RoomStatus.Cancelled, (await db.Rooms
            .SingleAsync(r => r.Id == lobby.Id)).Status);
        Assert.False(await db.Matches
            .AnyAsync(m => m.RoomId == room.Id));
    }

    [Fact]
    public async Task RestartCancelsStaleGames()
    {
        var room = await SeedAsync();
        await using var db = fixture.NewContext();

        await new RoomMaintenance(
            db,
            new RoomCoordinator(),
            new GameSessionService(TimeProvider.System, Options.Create(new GameOptions())),
            fixture.Factory.Services
                .GetRequiredService<RoomEvents>(),
            TimeProvider.System,
            Options.Create(new GameOptions())).CleanAsync(true, default);
        Assert.Equal(RoomStatus.Cancelled, (await db.Rooms
            .SingleAsync(r => r.Id == room.Id)).Status);
        Assert.False(await db.Matches
            .AnyAsync(m => m.RoomId == room.Id));
    }

    private sealed class FailSave : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            throw new IOException("Simulated database outage");
        }
    }
}
