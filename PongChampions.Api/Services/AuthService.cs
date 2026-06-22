using Microsoft.EntityFrameworkCore;
using PongChampions.Api.Common.Dtos.User;
using PongChampions.Data;
using PongChampions.Data.Entities;
using PongChampions.Data.Enums;

namespace PongChampions.Api.Services;

public class AuthService(AppDbContext context)
{
    public async Task RegisterAsync(UserRegisterDto dto)
    {
        var exists = await context.Users.AnyAsync(x => x.Username == dto.Username);

        if (exists)
            throw new InvalidOperationException("Username already exists");

        var user = new User
        {
            Username = dto.Username,
            DisplayName = dto.DisplayName,
            Country = dto.Country,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = Role.Player,
            CreatedAt = DateTime.UtcNow
        };

        var player = new Player
        {
            User = user,
            DisplayName = dto.DisplayName,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        context.Players.Add(player);

        await context.SaveChangesAsync();
    }

    public async Task<bool> LoginAsync(UserLoginDto dto)
    {
        var user = await context.Users.FirstOrDefaultAsync(x => x.Username == dto.Username);

        if (user is null) return false;

        var isValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);

        return isValid;
    }
}
