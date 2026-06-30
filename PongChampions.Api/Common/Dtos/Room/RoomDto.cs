using PongChampions.Data.Enums;

namespace PongChampions.Api.Common.Dtos.Room;

public class RoomDto : BaseDto
{
    public required string Code { get; set; }

    public Guid HostPlayerId { get; set; }
    public string HostDisplayName { get; set; } = string.Empty;

    public Guid? GuestPlayerId { get; set; }
    public string? GuestDisplayName { get; set; }

    public RoomStatus Status { get; set; }
}
