using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PongChampions.Api.Services;
using PongChampions.Data;

namespace PongChampions.Api.Hubs;

public class RoomHub(
    AppDbContext context,
    IGameSessionService gameSessionService) : Hub
{
    public async Task JoinRoom(string code, Guid? playerId = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new HubException("Room code is required.");

        code = code.Trim().ToUpperInvariant();

        var room = await context.Rooms.SingleOrDefaultAsync(r => r.Code == code);

        if (room is null)
            throw new HubException("Room not found.");

        await Groups.AddToGroupAsync(Context.ConnectionId, code);

        gameSessionService.RegisterConnection(
            Context.ConnectionId,
            code,
            playerId,
            room.HostPlayerId,
            room.GuestPlayerId);

        await Clients.Caller.SendAsync("JoinedRoom", new
        {
            code,
            playerId
        });
    }

    public async Task LeaveRoom(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new HubException("Room code is required.");

        code = code.Trim().ToUpperInvariant();

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, code);

        gameSessionService.RemoveConnection(Context.ConnectionId);

        await Clients.Caller.SendAsync("LeftRoom", code);
    }

    public async Task MovePaddle(string code, double y)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new HubException("Room code is required.");

        code = code.Trim().ToUpperInvariant();

        try
        {
            var gameState = gameSessionService.UpdatePaddle(
                Context.ConnectionId,
                code,
                y);

            await Clients.Group(code).SendAsync("GameStateUpdated", gameState);
        }
        catch (InvalidOperationException ex)
        {
            throw new HubException(ex.Message);
        }
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        gameSessionService.RemoveConnection(Context.ConnectionId);

        return base.OnDisconnectedAsync(exception);
    }
}
