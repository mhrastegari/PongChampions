using Microsoft.EntityFrameworkCore;
using PongChampions.Api.Common;
using PongChampions.Api.Common.Dtos.Room;
using PongChampions.Api.Common.Mappings;
using PongChampions.Data;
using PongChampions.Data.Entities;
using PongChampions.Data.Enums;
using System.Security.Claims;
using System.Security.Cryptography;

namespace PongChampions.Api.Services;

public sealed class RoomService(
    AppDbContext context,
    RoomCoordinator coordinator,
    RoomEvents events,
    IGameSessionService sessions,
    GuestTokenService guestTokens) : IRoomService
{
    public async Task<RoomDto> CreateRoomAsync(Guid userId)
    {
        await using var lease = await coordinator.BeginAsync(context);

        var hostPlayer = await GetPlayerAsync(userId);

        await EnsureAvailableAsync(hostPlayer.Id);

        string code;

        do
        {
            code = RandomNumberGenerator.GetString("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", 6);
        }
        while (await context.Rooms.AnyAsync(r => r.Code == code));

        var room = new Room
        {
            Code = code,
            HostPlayerId = hostPlayer.Id,
            HostPlayer = hostPlayer,
            Status = RoomStatus.WaitingForPlayers
        };

        context.Rooms.Add(room);

        await SaveAsync();

        return room.ToDto();
    }

    public async Task<RoomDto?> GetRoomAsync(string code)
    {
        code = NormalizeCode(code);

        var room = await Rooms()
            .AsNoTracking()
            .SingleOrDefaultAsync(r => r.Code == code);

        return room?.ToDto();
    }

    public async Task<JoinRoomResponseDto> JoinRoomAsync(string code, Guid? userId)
    {
        RoomDto roomDto;
        string? guestToken = null;

        await using (await coordinator.BeginAsync(context))
        {
            var room = await GetAsync(code);

            if (room.Status != RoomStatus.WaitingForPlayers || room.GuestPlayerId is not null)
                throw ApiException.Conflict("Room is full or no longer joinable.");

            Player guestPlayer;

            if (userId is not null)
            {
                guestPlayer = await GetPlayerAsync(userId.Value);

                if (room.HostPlayerId == guestPlayer.Id)
                    throw ApiException.Conflict("You cannot join your own room.");

                await EnsureAvailableAsync(guestPlayer.Id);
            }
            else
            {
                guestPlayer = new Player
                {
                    DisplayName = "Guest"
                };
                context.Players.Add(guestPlayer);

                guestToken = guestTokens.Issue(guestPlayer.Id, room.Code);
            }

            room.GuestPlayer = guestPlayer;
            room.GuestPlayerId = guestPlayer.Id;
            room.Status = RoomStatus.Ready;

            await SaveAsync();

            roomDto = room.ToDto();
        }

        // A guest credential is returned only to its creator, never broadcast.
        await events.SendAsync(roomDto.Code, "PlayerJoined", roomDto);

        return new(roomDto, guestToken);
    }

    public async Task<RoomDto> StartMatchAsync(string code, Guid userId)
    {
        RoomDto roomDto;

        await using (await coordinator.BeginAsync(context))
        {
            var room = await GetAsync(code);

            await RequireHostAsync(room, userId);

            if (room.Status != RoomStatus.Ready || room.GuestPlayerId is null)
                throw ApiException.Conflict("Room is not ready to start.");

            room.Status = RoomStatus.InGame;
            sessions.CreateSession(room.Code, room.MaxScore);

            try
            {
                await SaveAsync();
            }
            catch
            {
                sessions.RemoveSession(room.Code);

                throw;
            }

            roomDto = room.ToDto();
        }

        await events.SendAsync(roomDto.Code, "MatchStarted", roomDto);

        if (sessions.GetSession(roomDto.Code) is { } state)
            await events.SendAsync(roomDto.Code, "GameStateUpdated", state);

        return roomDto;
    }

    public async Task<RoomDto> CloseRoomAsync(string code, Guid userId)
    {
        RoomDto roomDto;

        await using (await coordinator.BeginAsync(context))
        {
            var room = await GetAsync(code);

            await RequireHostAsync(room, userId);

            if (room.Status == RoomStatus.Finished)
                throw ApiException.Conflict("Match is already complete.");

            room.Status = RoomStatus.Cancelled;

            await SaveAsync();

            sessions.RemoveSession(room.Code);

            roomDto = room.ToDto();
        }

        await events.SendAsync(roomDto.Code, "RoomClosed", roomDto);

        return roomDto;
    }

    public async Task<RoomDto> LeaveRoomAsync(string code, ClaimsPrincipal user)
    {
        RoomDto roomDto;

        await using (await coordinator.BeginAsync(context))
        {
            var room = await GetAsync(code);
            var playerId = await PlayerIdentity.ResolveAsync(user, room, context);

            if (playerId is null || playerId != room.GuestPlayerId)
                throw ApiException.Forbidden("Only the guest can leave; the host must close the room.");

            if (room.Status is RoomStatus.Finished or RoomStatus.Cancelled)
                throw ApiException.Conflict("Room has ended.");

            if (room.Status == RoomStatus.InGame)
                room.Status = RoomStatus.Cancelled;
            else
            {
                room.GuestPlayer = null;
                room.GuestPlayerId = null;
                room.Status = RoomStatus.WaitingForPlayers;
            }

            await SaveAsync();
            sessions.RevokePlayer(room.Code, playerId.Value);

            if (room.Status == RoomStatus.Cancelled)
                sessions.RemoveSession(room.Code);

            roomDto = room.ToDto();
        }

        var eventName = roomDto.Status == RoomStatus.Cancelled
            ? "RoomClosed"
            : "PlayerLeft";

        await events.SendAsync(roomDto.Code, eventName, roomDto);

        return roomDto;
    }

    public static string NormalizeCode(string code)
    {
        code = (code ?? "").Trim().ToUpperInvariant();

        if (code.Length != 6 || code.Any(c => !(c is >= 'A' and <= 'Z' or >= '0' and <= '9')))
            throw ApiException.Invalid("Room code must contain six letters or digits.");

        return code;
    }

    private IQueryable<Room> Rooms()
    {
        return context.Rooms
            .Include(r => r.HostPlayer)
            .Include(r => r.GuestPlayer);
    }

    private async Task<Room> GetAsync(string code)
    {
        code = NormalizeCode(code);

        return await Rooms()
            .SingleOrDefaultAsync(r => r.Code == code)
            ?? throw ApiException.NotFound("Room not found.");
    }

    private async Task<Player> GetPlayerAsync(Guid userId)
    {
        return await context.Players
            .SingleOrDefaultAsync(p => p.UserId == userId)
            ?? throw ApiException.NotFound("Player not found.");
    }

    private async Task RequireHostAsync(Room room, Guid userId)
    {
        var hostPlayer = await GetPlayerAsync(userId);

        if (room.HostPlayerId != hostPlayer.Id)
            throw ApiException.Forbidden("Only the host can perform this action.");
    }

    private async Task EnsureAvailableAsync(Guid playerId)
    {
        var alreadyInActiveRoom = await context.Rooms.AnyAsync(r =>
            (r.Status == RoomStatus.WaitingForPlayers ||
             r.Status == RoomStatus.Ready ||
             r.Status == RoomStatus.InGame) &&
            (r.HostPlayerId == playerId || r.GuestPlayerId == playerId));

        if (alreadyInActiveRoom)
            throw ApiException.Conflict("You are already in an active room.");
    }

    private async Task SaveAsync()
    {
        await context.SaveChangesAsync();
        await context.Database.CurrentTransaction!.CommitAsync();
    }
}
