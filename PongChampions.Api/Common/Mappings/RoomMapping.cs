using PongChampions.Api.Common.Dtos.Room;
using PongChampions.Data.Entities;

namespace PongChampions.Api.Common.Mappings;

public static class RoomMapping
{
    public static RoomDto ToDto(this Room room) => new()
    {
        Code = room.Code,
        HostPlayerId = room.HostPlayerId,
        GuestPlayerId = room.GuestPlayerId,
        Status = room.Status,
    };
}
