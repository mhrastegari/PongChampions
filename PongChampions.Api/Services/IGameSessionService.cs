using PongChampions.Api.Common.Dtos.Game;

namespace PongChampions.Api.Services;

public interface IGameSessionService
{
    GameStateDto CreateSession(string roomCode, int targetScore);
    GameStateDto? GetSession(string roomCode);
    void RegisterConnection(
        string connectionId,
        string roomCode,
        Guid? playerId,
        Guid hostPlayerId,
        Guid? guestPlayerId);
    void RemoveSession(string roomCode);
    void RemoveConnection(string connectionId);
    GameStateDto UpdatePaddle(
        string connectionId,
        string roomCode,
        double y);
    IReadOnlyList<GameStateDto> TickAll();
    string? GetConnectionRoom(string connectionId);
    void RevokePlayer(string roomCode, Guid playerId);
    IReadOnlyList<string> GetAbandonedRooms();
    void Clear();
}
