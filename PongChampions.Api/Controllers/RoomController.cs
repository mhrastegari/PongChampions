using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PongChampions.Api.Services;
using System.Security.Claims;

namespace PongChampions.Api.Controllers;

[ApiController]
[Route("api/rooms")]
public class RoomController(RoomService roomService) : ControllerBase
{
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create()
    {
        try
        {
            var userId = GetCurrentUserId();

            var room = await roomService.CreateRoomAsync(userId);

            return Ok(room);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{code}")]
    public async Task<IActionResult> Get([FromRoute] string code)
    {
        var room = await roomService.GetRoomAsync(code);

        if (room is null)
            return NotFound();

        return Ok(room);
    }

    [HttpPost("{code}/join")]
    public async Task<IActionResult> Join([FromRoute] string code)
    {
        try
        {
            var userId = TryGetCurrentUserId();

            var room = await roomService.JoinRoomAsync(code, userId);

            return Ok(room);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [Authorize]
    [HttpPost("{code}/start")]
    public async Task<IActionResult> Start([FromRoute] string code)
    {
        try
        {
            var userId = GetCurrentUserId();

            var room = roomService.StartMatchAsync(code, userId);

            return Ok(room);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [Authorize]
    [HttpPost("{code}/close")]
    public async Task<IActionResult> Close([FromRoute] string code)
    {
        try
        {
            var userId = GetCurrentUserId();

            var room = await roomService.CloseRoomAsync(code, userId);

            return Ok(room);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private Guid GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.Parse(userId!);
    }

    private Guid? TryGetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(value, out var userId)
            ? userId
            : null;
    }
}
