using PongChampions.Api.Common.Dtos.Room;
using System.Security.Claims;

namespace PongChampions.Api.Services;

public interface IRoomService
{
    Task<RoomDto> CreateRoomAsync(Guid userId);
    Task<RoomDto?> GetRoomAsync(string code);
    Task<JoinRoomResponseDto> JoinRoomAsync(string code, Guid? userId);
    Task<RoomDto> StartMatchAsync(string code, Guid userId);
    Task<RoomDto> CloseRoomAsync(string code, Guid userId);
    Task<RoomDto> LeaveRoomAsync(string code, ClaimsPrincipal user);
}
