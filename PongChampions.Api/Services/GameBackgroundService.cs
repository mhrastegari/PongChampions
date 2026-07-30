using Microsoft.AspNetCore.SignalR;
using PongChampions.Api.Hubs;

namespace PongChampions.Api.Services;

public class GameBackgroundService(
    IGameSessionService gameSessionService,
    IHubContext<RoomHub> roomHub) : BackgroundService
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
            }

            await Task.Delay(100, cancellationToken);
        }
    }
}
