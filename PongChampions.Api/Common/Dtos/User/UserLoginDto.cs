using System.ComponentModel.DataAnnotations;


namespace PongChampions.Api.Common.Dtos.User;

public class UserLoginDto
{
    [Required]
    [StringLength(32)]
    public required string Username { get; set; }

    [Required]
    [StringLength(72)]
    public required string Password { get; set; }
}
