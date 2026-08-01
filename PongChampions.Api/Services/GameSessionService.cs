using PongChampions.Api.Common.Dtos.Game;
using PongChampions.Api.Common.Enums;
using System.Collections.Concurrent;

namespace PongChampions.Api.Services;

public class GameSessionService : IGameSessionService
{
    private const double HostPaddleX = 0.05;
    private const double GuestPaddleX = 0.95;
    private const double PaddleHalfHeight = 0.12;

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

    public void RemoveSession(string roomCode)
    {
        sessions.TryRemove(roomCode, out _);
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

    public IReadOnlyList<GameStateDto> TickAll()
    {
        var updatedStates = new List<GameStateDto>();

        foreach (var gameState in sessions.Values)
        {
            if (!gameState.IsRunning)
                continue;

            lock (gameState)
            {
                MoveBall(gameState);
                BounceFromTopBottom(gameState);
                HandlePaddleCollisionOrScore(gameState);

                updatedStates.Add(gameState);
            }
        }

        return updatedStates;
    }

    private static void MoveBall(GameStateDto gameState)
    {
        gameState.Ball.X += gameState.Ball.VelocityX;
        gameState.Ball.Y += gameState.Ball.VelocityY;
    }

    private static void BounceFromTopBottom(GameStateDto gameState)
    {
        if (gameState.Ball.Y <= 0 || gameState.Ball.Y >= 1)
        {
            gameState.Ball.VelocityY *= -1;
            gameState.Ball.Y = Math.Clamp(gameState.Ball.Y, 0, 1);
        }
    }

    private static void HandlePaddleCollisionOrScore(GameStateDto gameState)
    {
        var ball = gameState.Ball;

        if (ball.VelocityX < 0 && ball.X <= HostPaddleX)
        {
            if (IsPaddleHit(ball.Y, gameState.HostPaddle.Y))
            {
                ball.X = HostPaddleX;
                ball.VelocityX = Math.Abs(ball.VelocityX);
                return;
            }

            if (ball.X <= 0)
            {
                gameState.GuestScore++;
                ResetBall(gameState, directionX: 1);
            }

            return;
        }

        if (ball.VelocityX > 0 && ball.X >= GuestPaddleX)
        {
            if (IsPaddleHit(ball.Y, gameState.GuestPaddle.Y))
            {
                ball.X = GuestPaddleX;
                ball.VelocityX = -Math.Abs(ball.VelocityX);
                return;
            }

            if (ball.X >= 1)
            {
                gameState.HostScore++;
                ResetBall(gameState, directionX: -1);
            }
        }
    }

    private static bool IsPaddleHit(double ballY, double paddleY)
    {
        return Math.Abs(ballY - paddleY) <= PaddleHalfHeight;
    }

    private static void ResetBall(GameStateDto gameState, int directionX)
    {
        gameState.Ball.X = 0.5;
        gameState.Ball.Y = 0.5;

        gameState.Ball.VelocityX = 0.01 * directionX;
        gameState.Ball.VelocityY = Random.Shared.NextDouble() > 0.5 ? 0.01 : -0.01;
    }
}
