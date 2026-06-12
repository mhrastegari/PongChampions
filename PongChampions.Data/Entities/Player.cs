namespace PongChampions.Data.Entities;

public class Player : BaseEntity
{
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string? DisplayName { get; set; }
}
