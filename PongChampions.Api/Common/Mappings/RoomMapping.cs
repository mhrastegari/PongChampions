using PongChampions.Api.Common.Dtos.Room;
using PongChampions.Data.Entities;

namespace PongChampions.Api.Common.Mappings;

public static class RoomMapping
{
    public static RoomDto ToDto(this Room room) => new()
    {
        Id = room.Id,
        CreatedAt = room.CreatedAt,
        Code = room.Code,
        HostPlayerId = room.HostPlayerId,
        HostDisplayName = room.HostPlayer.DisplayName,
        GuestPlayerId = room.GuestPlayerId,
        GuestDisplayName = room.GuestPlayer?.DisplayName,
        Status = room.Status,
    };
}
