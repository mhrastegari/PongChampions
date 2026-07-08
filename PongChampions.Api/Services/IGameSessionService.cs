using PongChampions.Api.Common.Dtos.Game;

namespace PongChampions.Api.Services;

public interface IGameSessionService
{
    GameStateDto CreateSession(string roomCode);
    GameStateDto? GetSession(string roomCode);
    void RemoveSession(string roomCode);
}