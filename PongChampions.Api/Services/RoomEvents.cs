using Microsoft.AspNetCore.SignalR;
using PongChampions.Api.Hubs;

namespace PongChampions.Api.Services;

public sealed class RoomEvents(
    IHubContext<RoomHub> hub,
    ILogger<RoomEvents> logger)
{
    public async Task SendAsync(
        string code,
        string name,
        object payload,
        CancellationToken token = default)
    {
        try
        {
            await hub.Clients.Group(code)
                .SendAsync(name, payload, token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not send {Event} to room {RoomCode}", name, code);
        }
    }
}
