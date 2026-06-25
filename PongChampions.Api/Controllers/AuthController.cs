using Microsoft.AspNetCore.Mvc;
using PongChampions.Api.Common.Dtos.User;
using PongChampions.Api.Services;

namespace PongChampions.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserRegisterDto dto)
    {
        await authService.RegisterAsync(dto);
        return Ok();
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserLoginDto dto)
    {
        var result = await authService.LoginAsync(dto);

        if (result is null) return Unauthorized();

        return Ok(result);
    }
}
