using Microsoft.AspNetCore.SignalR;

namespace PongChampions.Api.Hubs;

public class RoomHub : Hub
{
    public async Task JoinRoom(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new HubException("Room code is required.");

        code = code.Trim().ToUpperInvariant();

        await Groups.AddToGroupAsync(Context.ConnectionId, code);

        await Clients.Caller.SendAsync("JoinedRoom", code);
    }

    public async Task LeaveRoom(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new HubException("Room code is required.");

        code = code.Trim().ToUpperInvariant();

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, code);

        await Clients.Caller.SendAsync("LeftRoom", code);
    }
}
