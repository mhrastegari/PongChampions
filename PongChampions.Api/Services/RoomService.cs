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
            await context.Players.SingleOrDefaultAsync(p => p.UserId == userId);

        if (hostPlayer is null)
            throw new InvalidOperationException("Player not found!");

        var alreadyInActiveRoom =
            await context.Rooms.AnyAsync(r =>
                r.Status != RoomStatus.Finished &&
                (r.HostPlayerId == hostPlayer.Id || r.GuestPlayerId == hostPlayer.Id));

        if (alreadyInActiveRoom)
            throw new InvalidOperationException("You are already in an active room.");

        var code = await GenerateRoomCodeAsync();

        var room = new Room
        {
            Code = code,
            HostPlayerId = hostPlayer.Id,
            HostPlayer = hostPlayer,
            Status = RoomStatus.WaitingForPlayers,
        };

        context.Rooms.Add(room);

        await context.SaveChangesAsync();

        return room.ToDto();
    }

    public async Task<RoomDto> JoinRoomAsync(string code, Guid? userId)
    {
        var room =
            await context.Rooms
                .Include(r => r.HostPlayer)
                .Include(r => r.GuestPlayer)
                .SingleOrDefaultAsync(r => r.Code == code);

        if (room is null)
            throw new InvalidOperationException("Room not found!");

        if (room.Status is not RoomStatus.WaitingForPlayers)
            throw new InvalidOperationException("Room is not joinable.");

        if (room.GuestPlayerId is not null)
            throw new InvalidOperationException("The room is full.");

        Player guestPlayer;

        if (userId is not null)
        {
            guestPlayer =
                await context.Players.SingleOrDefaultAsync(p => p.UserId == userId.Value)
                    ?? throw new InvalidOperationException("Player not found!");

            if (room.HostPlayerId == guestPlayer.Id)
                throw new InvalidOperationException("You cannot join your own room.");

            var alreadyInActiveRoom =
                await context.Rooms.AnyAsync(r =>
                    r.Id != room.Id &&
                    r.Status != RoomStatus.Finished &&
                    (r.HostPlayerId == guestPlayer.Id || r.GuestPlayerId == guestPlayer.Id));

            if (alreadyInActiveRoom)
                throw new InvalidOperationException("You are already in an active room.");
        }
        else
        {
            guestPlayer = new()
            {
                DisplayName = "Guest",
            };

            context.Players.Add(guestPlayer);
        }

        room.GuestPlayerId = guestPlayer.Id;
        room.GuestPlayer = guestPlayer;
        room.Status = RoomStatus.Ready;

        await context.SaveChangesAsync();

        return room.ToDto();
    }

    public async Task<RoomDto?> GetRoomAsync(string code)
    {
        var room =
            await context.Rooms
                .Include(r => r.HostPlayer)
                .Include(r => r.GuestPlayer)
                .SingleOrDefaultAsync(r => r.Code == code);

        return room?.ToDto();
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
