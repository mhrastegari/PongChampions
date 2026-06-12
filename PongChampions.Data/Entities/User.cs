using PongChampions.Data.Enums;

namespace PongChampions.Data.Entities;

public class User : BaseEntity
{
    public string Username { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string Country { get; set; } = null!;
    public Role Role { get; set; } = Role.Player;
}
