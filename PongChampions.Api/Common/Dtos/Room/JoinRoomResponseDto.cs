namespace PongChampions.Api.Common.Dtos.Room;

public sealed record JoinRoomResponseDto(
    RoomDto Room,
    string? GuestToken);
