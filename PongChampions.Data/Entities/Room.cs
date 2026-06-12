using PongChampions.Data.Enums;

namespace PongChampions.Data.Entities;

public class Room : BaseEntity
{
    public string Code { get; set; } = null!;
    public Guid HostPlayerId { get; set; }
    public RoomStatus Status { get; set; }
}
