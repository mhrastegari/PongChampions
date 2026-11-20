using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PongChampions.Api.Common;
using PongChampions.Api.Services;

namespace PongChampions.Api.Controllers;

[ApiController]
[Route("api/rooms")]
public sealed class RoomController(IRoomService rooms) : ControllerBase
{
    [Authorize(Policy = "RegisteredPlayer")]
    [HttpPost]
    public async Task<IActionResult> Create()
    {
        var userId = PlayerIdentity.RegisteredUserId(User);

        var room = await rooms.CreateRoomAsync(userId);

        return Ok(room);
    }

    [HttpGet("{code}")]
    public async Task<IActionResult> Get(string code)
    {
        var room = await rooms.GetRoomAsync(code);

        if (room is null)
            throw ApiException.NotFound("Room not found.");

        return Ok(room);
    }

    [HttpPost("{code}/join")]
    public async Task<IActionResult> Join(string code)
    {
        if (User.FindFirst("identity_kind")?.Value == "guest")
            throw ApiException.Forbidden("Use your guest credential to reconnect through SignalR.");

        Guid? userId = User.Identity?.IsAuthenticated == true
            ? PlayerIdentity.RegisteredUserId(User)
            : null;

        var result = await rooms.JoinRoomAsync(code, userId);

        return Ok(result);
    }

    [Authorize(Policy = "RegisteredPlayer")]
    [HttpPost("{code}/start")]
    public async Task<IActionResult> Start(string code)
    {
        var userId = PlayerIdentity.RegisteredUserId(User);

        var room = await rooms.StartMatchAsync(code, userId);

        return Ok(room);
    }

    [Authorize(Policy = "RegisteredPlayer")]
    [HttpPost("{code}/close")]
    public async Task<IActionResult> Close(string code)
    {
        var userId = PlayerIdentity.RegisteredUserId(User);

        var room = await rooms.CloseRoomAsync(code, userId);

        return Ok(room);
    }

    [Authorize]
    [HttpPost("{code}/leave")]
    public async Task<IActionResult> Leave(string code)
    {
        var room = await rooms.LeaveRoomAsync(code, User);

        return Ok(room);
    }
}
