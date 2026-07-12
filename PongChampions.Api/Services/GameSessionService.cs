using PongChampions.Api.Common.Dtos.Game;
using PongChampions.Api.Common.Enums;
using System.Collections.Concurrent;

namespace PongChampions.Api.Services;

public class GameSessionService : IGameSessionService
{
    private readonly ConcurrentDictionary<string, GameStateDto> sessions = new();

    private readonly ConcurrentDictionary<string, PlayerConnection> connections = new();

    private record PlayerConnection(
        string RoomCode,
        Guid? PlayerId,
        GamePlayerSide? Side);

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

    public void RegisterConnection(
        string connectionId,
        string roomCode,
        Guid? playerId,
        Guid hostPlayerId,
        Guid? guestPlayerId)
    {
        GamePlayerSide? side = null;

        if (playerId == hostPlayerId)
            side = GamePlayerSide.Host;
        else if (guestPlayerId is not null && playerId == guestPlayerId)
            side = GamePlayerSide.Guest;

        connections[connectionId] = new PlayerConnection(
            RoomCode: roomCode,
            PlayerId: playerId,
            Side: side);
    }

    public void RemoveConnection(string connectionId)
    {
        connections.TryRemove(connectionId, out _);
    }

    public GameStateDto UpdatePaddle(string connectionId, string roomCode, double y)
    {
        if (!connections.TryGetValue(connectionId, out var connection))
            throw new InvalidOperationException("Connection is not registered.");

        if (connection.RoomCode != roomCode)
            throw new InvalidOperationException("Connection is not joined to this room.");

        if (connection.Side is null)
            throw new InvalidOperationException("Spectators cannot move paddles.");

        if (!sessions.TryGetValue(roomCode, out var gameState))
            throw new InvalidOperationException("Game session not found.");

        y = Math.Clamp(y, 0, 1);

        lock (gameState)
        {
            if (connection.Side == GamePlayerSide.Host)
                gameState.HostPaddle.Y = y;
            else
                gameState.GuestPaddle.Y = y;
        }

        return gameState;
    }

    public void RemoveSession(string roomCode)
    {
        sessions.TryRemove(roomCode, out _);
    }
}
