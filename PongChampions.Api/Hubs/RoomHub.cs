using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PongChampions.Api.Common;
using PongChampions.Api.Services;
using PongChampions.Data;
using PongChampions.Data.Enums;

namespace PongChampions.Api.Hubs;

public sealed class RoomHub(
    AppDbContext context,
    IGameSessionService sessions,
    RoomCoordinator coordinator) : Hub
{
    // Identity is derived exclusively from the validated bearer token.
    public async Task JoinRoom(string code)
    {
        try
        {
            code = RoomService.NormalizeCode(code);

            await using var lease = await coordinator.BeginAsync(context, Context.ConnectionAborted);

            var room = await context.Rooms
                .SingleOrDefaultAsync(r => r.Code == code, Context.ConnectionAborted)
                ?? throw ApiException.NotFound("Room not found.");

            if (room.Status is RoomStatus.Cancelled or RoomStatus.Finished)
                throw ApiException.Conflict("Room has ended.");

            var playerId = await PlayerIdentity.ResolveAsync(Context.User!, room, context);

            if (sessions.GetConnectionRoom(Context.ConnectionId) is { } previous)
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, previous);

            await Groups.AddToGroupAsync(Context.ConnectionId, code);

            sessions.RegisterConnection(
                Context.ConnectionId,
                code,
                playerId,
                room.HostPlayerId,
                room.GuestPlayerId);

            await context.Database.CurrentTransaction!.CommitAsync(Context.ConnectionAborted);

            await Clients.Caller
                .SendAsync("JoinedRoom", new { code, playerId });

            if (sessions.GetSession(code) is { } state)
                await Clients.Caller
                    .SendAsync("GameStateUpdated", state);
        }
        catch (ApiException exception)
        {
            throw new HubException(exception.Message);
        }
    }

    public async Task LeaveRoom(string code)
    {
        try
        {
            code = RoomService.NormalizeCode(code);
        }
        catch (ApiException exception)
        {
            throw new HubException(exception.Message);
        }

        if (sessions.GetConnectionRoom(Context.ConnectionId) != code)
            throw new HubException("Connection is not joined to this room.");

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, code);
        sessions.RemoveConnection(Context.ConnectionId);
        await Clients.Caller
            .SendAsync("LeftRoom", code);
    }

    public async Task MovePaddle(string code, double y)
    {
        try
        {
            var state = sessions.UpdatePaddle(Context.ConnectionId, RoomService.NormalizeCode(code), y);

            await Clients.Caller
                .SendAsync("PaddleUpdated", state);
        }
        catch (ApiException exception)
        {
            throw new HubException(exception.Message);
        }
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        sessions.RemoveConnection(Context.ConnectionId);

        return base.OnDisconnectedAsync(exception);
    }
}
