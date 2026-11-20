using System.ComponentModel.DataAnnotations;


namespace PongChampions.Api.Common.Dtos.User;

public class UserRegisterDto
{
    [Required]
    [StringLength(32, MinimumLength = 3)]
    [RegularExpression("^[a-zA-Z0-9_]+$")]
    public required string Username { get; set; }

    [Required]
    [StringLength(72, MinimumLength = 10)]
    public required string Password { get; set; }

    [Required]
    [StringLength(32)]
    public required string DisplayName { get; set; }

    [StringLength(64)]
    public string? Country { get; set; }
}
