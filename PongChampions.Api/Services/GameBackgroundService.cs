using Microsoft.Extensions.Options;

namespace PongChampions.Api.Services;

public sealed class GameBackgroundService(
    IGameSessionService sessions,
    RoomEvents events,
    IServiceScopeFactory scopes,
    IOptions<GameOptions> options,
    TimeProvider clock,
    ILogger<GameBackgroundService> logger) : BackgroundService
{
    public override async Task StartAsync(CancellationToken token)
    {
        // A restart cannot recover in-memory physics. Cancel stale games explicitly.
        using var scope = scopes.CreateScope();

        var maintenance = scope.ServiceProvider.GetRequiredService<RoomMaintenance>();

        await maintenance.CleanAsync(true, token);

        await base.StartAsync(token);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(options.Value.TickMilliseconds), clock);

        var lastMaintenance = clock.GetUtcNow();
        var nextAttempt = new Dictionary<string, DateTimeOffset>();

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                foreach (var gameState in sessions.TickAll())
                {
                    if (gameState.IsRunning)
                    {
                        await events.SendAsync(gameState.RoomCode, "GameStateUpdated", gameState, stoppingToken);

                        continue;
                    }

                    if (nextAttempt.TryGetValue(gameState.RoomCode, out var next) && next > clock.GetUtcNow())
                        continue;

                    try
                    {
                        using var scope = scopes.CreateScope();

                        var matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

                        var result = await matchService.FinishMatchAsync(gameState, stoppingToken);

                        if (result != MatchCompletionResult.Cancelled)
                            await events.SendAsync(gameState.RoomCode, "MatchFinished", gameState, stoppingToken);

                        sessions.RemoveSession(gameState.RoomCode);
                        nextAttempt.Remove(gameState.RoomCode);
                        logger.LogInformation("Room {RoomCode} completed with result {Result}", gameState.RoomCode, result);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        nextAttempt[gameState.RoomCode] = clock.GetUtcNow().AddSeconds(2);
                        logger.LogError(exception, "Persisting room {RoomCode} failed; retrying in two seconds", gameState.RoomCode);
                    }
                }

                if (clock.GetUtcNow() - lastMaintenance < TimeSpan.FromSeconds(1))
                    continue;

                try
                {
                    using var scope = scopes.CreateScope();

                    var maintenance = scope.ServiceProvider.GetRequiredService<RoomMaintenance>();

                    await maintenance.CleanAsync(false, stoppingToken);

                    lastMaintenance = clock.GetUtcNow();

                    foreach (var code in nextAttempt.Keys
                        .Where(c => sessions.GetSession(c) is null)
                        .ToArray())
                        nextAttempt.Remove(code);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    lastMaintenance = clock.GetUtcNow();
                    logger.LogError(exception, "Room cleanup failed; will retry");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    public override async Task StopAsync(CancellationToken token)
    {
        await base.StopAsync(token);

        try
        {
            // Flush completed results first. Active games are cancellations, never wins.
            foreach (var gameState in sessions.TickAll()
                .Where(s => !s.IsRunning))
            {
                using var scope = scopes.CreateScope();

                var matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();

                await matchService.FinishMatchAsync(gameState, token);
            }

            using var cleanup = scopes.CreateScope();

            var maintenance = cleanup.ServiceProvider.GetRequiredService<RoomMaintenance>();

            await maintenance.CleanAsync(true, token);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Shutdown cleanup failed; stale rooms will be cancelled on restart");
        }
        finally
        {
            sessions.Clear();
        }
    }
}
