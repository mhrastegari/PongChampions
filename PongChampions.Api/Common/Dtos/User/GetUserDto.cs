using PongChampions.Data.Enums;

namespace PongChampions.Api.Common.Dtos.User;

public class GetUserDto : BaseDto
{
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; }  = string.Empty;
    public string Country { get; set; }  = string.Empty;
    public Role Role { get; set; } = Role.Player;
}
