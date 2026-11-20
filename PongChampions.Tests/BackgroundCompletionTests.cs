using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PongChampions.Api.Services;
using PongChampions.Data;
using PongChampions.Data.Entities;
using PongChampions.Data.Enums;

namespace PongChampions.Tests;

[Collection("backend")]
public sealed class BackgroundCompletionTests(BackendFixture fixture)
{
    [Fact]
    public async Task BackgroundLoopRetriesPersistenceFailureAndKeepsOtherRoomsRunning()
    {
        var options = Options.Create(new GameOptions
        {
            TickMilliseconds = 10
        });
        var sessions = new GameSessionService(TimeProvider.System, options);
        var events = fixture.Factory.Services
            .GetRequiredService<RoomEvents>();
        var interceptor = new FailFirstMatchSave();
        var services = new ServiceCollection();

        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton<IOptions<GameOptions>>(options);
        services.AddSingleton<IGameSessionService>(sessions);
        services.AddSingleton(events);
        services.AddSingleton<RoomCoordinator>();
        services.AddDbContext<AppDbContext>(o => o.UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(interceptor));
        services.AddScoped<IMatchService, MatchService>();
        services.AddScoped<RoomMaintenance>();

        await using var provider = services.BuildServiceProvider();

        var background = new GameBackgroundService(
            sessions,
            events,
            provider.GetRequiredService<IServiceScopeFactory>(),
            options,
            TimeProvider.System,
            NullLogger<GameBackgroundService>.Instance);

        await background.StartAsync(default);

        try
        {
            await using var db = fixture.NewContext();

            var room = new Room
            {
                Code = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant(),
                MaxScore = 1,
                Status = RoomStatus.InGame,
                HostPlayer = new Player
                {
                    DisplayName = "Host"
                },
                GuestPlayer = new Player
                {
                    DisplayName = "Guest"
                }
            };

            db.Rooms.Add(room);
            await db.SaveChangesAsync();
            sessions.CreateSession(room.Code, 1);
            sessions.CreateSession("OTHER1", 1000);

            var deadline = DateTime.UtcNow.AddSeconds(10);

            while (sessions.GetSession(room.Code) is not null && DateTime.UtcNow < deadline)
                await Task.Delay(50);

            Assert.Null(sessions.GetSession(room.Code));
            Assert.True(interceptor.Attempts >= 2);
            Assert.True(sessions.GetSession("OTHER1")!.IsRunning);
            await db.Entry(room).ReloadAsync();
            Assert.Equal(RoomStatus.Finished, room.Status);

            var match = Assert.Single(await db.Matches
                .Where(m => m.RoomId == room.Id)
                .ToListAsync());

            Assert.Equal(1, match.Player1Score + match.Player2Score);
        }
        finally
        {
            await background.StopAsync(default);
            background.Dispose();
        }

        Assert.Empty(sessions.TickAll());
    }

    private sealed class FailFirstMatchSave : SaveChangesInterceptor
    {
        private int attempts;
        public int Attempts => attempts;

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<Match>().Any(e => e.State == EntityState.Added) &&
                Interlocked.Increment(ref attempts) == 1)
                throw new IOException("Simulated database outage");

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}
