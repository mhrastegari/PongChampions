using Microsoft.EntityFrameworkCore;
using PongChampions.Api.Common.Dtos.Room;
using PongChampions.Api.Common.Mappings;
using PongChampions.Data;
using PongChampions.Data.Entities;
using PongChampions.Data.Enums;

namespace PongChampions.Api.Services;

public class RoomService(AppDbContext context)
{
    public async Task<RoomDto> CreateRoomAsync(Guid userId)
    {
        var hostPlayer =
            await context.Players.FirstOrDefaultAsync(p => p.UserId == userId);

        if (hostPlayer is null) 
            throw new Exception("Player not found!");

        var alreadyHosting =
            await context.Rooms.AnyAsync(r => r.HostPlayerId == hostPlayer.Id && r.Status != RoomStatus.Finished);

        if (alreadyHosting)
            throw new Exception("You already have an active room.");

        var code = await GenerateRoomCodeAsync();

        var room = new Room
        {
            Code = code,
            HostPlayerId = hostPlayer.Id,
            Status = RoomStatus.WaitingForPlayers,
            CreatedAt = DateTime.UtcNow,
        };

        context.Rooms.Add(room);

        await context.SaveChangesAsync();

        return room.ToDto();
    }

    public async Task<RoomDto> JoinRoomAsync(string code, Guid? userId)
    {
        var room = 
            await context.Rooms.SingleOrDefaultAsync(r => r.Code == code);

        if (room is null) 
            throw new Exception("Room not found!");

        if (room.GuestPlayerId is not null) 
            throw new Exception("The Room is full!");

        Player guestPlayer;

        if (userId is not null)
        {
            guestPlayer =
                await context.Players.SingleOrDefaultAsync(p => p.UserId == userId.Value)
                    ?? throw new Exception("Player not found!");

            if (room.HostPlayerId == guestPlayer.Id)
                throw new Exception("You cannot join your own room.");
        }
        else
        {
            guestPlayer = new()
            {
                DisplayName = "Guest",
                CreatedAt = DateTime.UtcNow
            };

            context.Players.Add(guestPlayer);
        }

        room.GuestPlayerId = guestPlayer.Id;
        room.Status = RoomStatus.Ready;

        await context.SaveChangesAsync();

        return room.ToDto();
    }

    private async Task<string> GenerateRoomCodeAsync()
    {
        string code;
        const int roomCodeLength = 6;
        const string roomCodeCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        do
        {
            code = string.Concat(
                Enumerable.Range(0, roomCodeLength)
                    .Select(_ => roomCodeCharacters[
                        Random.Shared.Next(roomCodeCharacters.Length)]));
        }
        while (await context.Rooms.AnyAsync(r => r.Code == code));

        return code;
    }
}
