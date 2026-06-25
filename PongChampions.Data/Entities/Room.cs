using PongChampions.Data.Enums;

namespace PongChampions.Data.Entities;

public class Room : BaseEntity
{
    public required string Code { get; set; }
    public Guid HostPlayerId { get; set; }
    public Player HostPlayer { get; set; } = null!;
    public Guid? GuestPlayerId { get; set; }
    public Player? GuestPlayer { get; set; }
    public RoomStatus Status { get; set; }
}
