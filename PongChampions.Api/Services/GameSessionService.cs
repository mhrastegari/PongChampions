using PongChampions.Api.Common.Dtos.Game;
using System.Collections.Concurrent;

namespace PongChampions.Api.Services;

public class GameSessionService : IGameSessionService
{
    private readonly ConcurrentDictionary<string, GameStateDto> sessions = new();

    public GameStateDto CreateSession(string roomCode)
    {
        var gameState = new GameStateDto
        {
            RoomCode = roomCode,
            HostPaddle = new()
            {
                Y = 0.5
            },
            GuestPaddle = new()
            {
                Y = 0.5
            },
            Ball = new()
            {
                X = 0.5,
                Y = 0.5,
                VelocityX = 0.01,
                VelocityY = 0.01,
            },
            HostScore = 0,
            GuestScore = 0,
            IsRunning = true,
        };

        sessions[roomCode] = gameState;

        return gameState;
    }

    public GameStateDto? GetSession(string roomCode)
    {
        return sessions.TryGetValue(roomCode, out var gameState)
            ? gameState
            : null;
    }

    public void RemoveSession(string roomCode)
    {
        sessions.TryRemove(roomCode, out _);
    }
}
