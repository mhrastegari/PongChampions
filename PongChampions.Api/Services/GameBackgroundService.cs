using Microsoft.AspNetCore.SignalR;
using PongChampions.Api.Hubs;

namespace PongChampions.Api.Services;

public class GameBackgroundService(
    IGameSessionService gameSessionService,
    IHubContext<RoomHub> roomHub,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var updatedStates = gameSessionService.TickAll();  

            foreach (var gameState in updatedStates)
            {
                await roomHub.Clients
                    .Group(gameState.RoomCode)
                    .SendAsync("GameStateUpdated", gameState, cancellationToken);

                if (!gameState.IsRunning)
                {
                    using var scope = scopeFactory.CreateScope();

                    var matchService =
                        scope.ServiceProvider.GetRequiredService<IMatchService>();

                    await matchService.FinishMatchAsync(
                        gameState,
                        cancellationToken);

                    await roomHub.Clients
                        .Group(gameState.RoomCode)
                        .SendAsync(
                            "MatchFinished",
                            gameState,
                            cancellationToken);

                    gameSessionService.RemoveSession(gameState.RoomCode);
                }
            }

            await Task.Delay(100, cancellationToken);
        }
    }
}
