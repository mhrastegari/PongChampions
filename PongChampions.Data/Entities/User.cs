using PongChampions.Data.Enums;

namespace PongChampions.Data.Entities;

public class User : BaseEntity
{
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public required string DisplayName { get; set; }
    public string? Country { get; set; }
    public Role Role { get; set; } = Role.Player;
}
