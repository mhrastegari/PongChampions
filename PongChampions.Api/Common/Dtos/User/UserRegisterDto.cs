namespace PongChampions.Api.Common.Dtos.User;

public class UserRegisterDto
{
    public required string Username { get; set; }
    public required string Password { get; set; }
    public required string DisplayName { get; set; }
    public string? Country { get; set; }
}
