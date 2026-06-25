namespace PongChampions.Data.Entities;

public class Match : BaseEntity
{
    public Guid RoomId { get; set; }

    public Guid Player1Id { get; set; }
    public Guid Player2Id { get; set; }

    public Guid WinnerPlayerId { get; set; }

    public int Player1Score { get; set; }
    public int Player2Score { get; set; }

    public DateTime? FinishedAt { get; set; }
}
