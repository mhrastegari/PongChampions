using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PongChampions.Api.Common.Dtos.User;
using PongChampions.Data;
using PongChampions.Data.Entities;
using PongChampions.Data.Enums;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PongChampions.Api.Services;

public class AuthService(
    AppDbContext context,
    IConfiguration configuration)
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
        };

        var player = new Player
        {
            User = user,
            DisplayName = dto.DisplayName,
        };

        context.Users.Add(user);
        context.Players.Add(player);

        await context.SaveChangesAsync();
    }

    public async Task<AuthResponseDto?> LoginAsync(UserLoginDto dto)
    {
        var user = await context.Users.FirstOrDefaultAsync(x => x.Username == dto.Username);

        if (user is null) return null;

        var isValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);

        if (isValid is false) return null;

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:key"]!));

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials);

        return new()
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token)
        };
    }
}
