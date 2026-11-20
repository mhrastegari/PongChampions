using Microsoft.Extensions.Options;
using PongChampions.Api.Common;
using PongChampions.Api.Common.Dtos.Game;
using PongChampions.Api.Common.Enums;

namespace PongChampions.Api.Services;

public sealed class GameSessionService(
    TimeProvider clock,
    IOptions<GameOptions> options) : IGameSessionService
{
    private const double HostPaddleX = 0.05;
    private const double GuestPaddleX = 0.95;
    private const double PaddleHalfHeight = 0.12;

    private readonly object sync = new();

    private readonly Dictionary<string, Session> sessions = new();

    private readonly Dictionary<string, PlayerConnection> connections = new();


    private sealed class Session(
        GameStateDto gameState,
        DateTimeOffset now)
    {
        public GameStateDto State { get; } = gameState;
        public DateTimeOffset HostLastSeen { get; set; } = now;
        public DateTimeOffset GuestLastSeen { get; set; } = now;
    }

    private sealed record PlayerConnection(
        string RoomCode,
        Guid? PlayerId,
        GamePlayerSide? Side);

    public GameStateDto CreateSession(string roomCode, int targetScore)
    {
        if (targetScore <= 0)
            throw ApiException.Invalid("Target score must be positive.");

        lock (sync)
        {
            if (sessions.ContainsKey(roomCode))
                throw ApiException.Conflict("Game session already exists.");

            var gameState = new GameStateDto
            {
                RoomCode = roomCode,
                TargetScore = targetScore,
                IsRunning = true,
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
                    VelocityY = 0.01
                }
            };

            sessions.Add(roomCode, new Session(gameState, clock.GetUtcNow()));

            return Snapshot(gameState);
        }
    }

    public GameStateDto? GetSession(string roomCode)
    {
        lock (sync)
        {
            return sessions.TryGetValue(roomCode, out var session)
                ? Snapshot(session.State)
                : null;
        }
    }

    public string? GetConnectionRoom(string connectionId)
    {
        lock (sync)
        {
            return connections.GetValueOrDefault(connectionId)?.RoomCode;
        }
    }

    public void RegisterConnection(
        string connectionId,
        string roomCode,
        Guid? playerId,
        Guid hostPlayerId,
        Guid? guestPlayerId)
    {
        lock (sync)
        {
            GamePlayerSide? side = null;

            if (playerId == hostPlayerId)
                side = GamePlayerSide.Host;
            else if (playerId is not null && playerId == guestPlayerId)
                side = GamePlayerSide.Guest;

            RemoveConnection(connectionId);
            connections[connectionId] = new(roomCode, playerId, side);
            Touch(roomCode, side);
        }
    }

    public void RemoveConnection(string connectionId)
    {
        lock (sync)
        {
            if (connections.Remove(connectionId, out var connection))
                Touch(connection.RoomCode, connection.Side);
        }
    }

    public void RevokePlayer(string roomCode, Guid playerId)
    {
        lock (sync)
        {
            var connectionIds = connections
                .Where(c => c.Value.RoomCode == roomCode && c.Value.PlayerId == playerId)
                .Select(c => c.Key)
                .ToArray();

            foreach (var connectionId in connectionIds)
                RemoveConnection(connectionId);
        }
    }

    public void RemoveSession(string roomCode)
    {
        lock (sync)
        {
            sessions.Remove(roomCode);

            var connectionIds = connections
                .Where(c => c.Value.RoomCode == roomCode)
                .Select(c => c.Key)
                .ToArray();

            foreach (var connectionId in connectionIds)
                connections.Remove(connectionId);
        }
    }

    public void Clear()
    {
        lock (sync)
        {
            sessions.Clear();
            connections.Clear();
        }
    }

    public GameStateDto UpdatePaddle(
        string connectionId,
        string roomCode,
        double y)
    {
        if (!double.IsFinite(y) || y < 0 || y > 1)
            throw ApiException.Invalid("Paddle Y must be a finite number between 0 and 1.");

        lock (sync)
        {
            if (!connections.TryGetValue(connectionId, out var connection) ||
                connection.RoomCode != roomCode)
                throw ApiException.Forbidden("Connection is not joined to this room.");

            if (connection.Side is null)
                throw ApiException.Forbidden("Spectators cannot move paddles.");

            if (!sessions.TryGetValue(roomCode, out var session))
                throw ApiException.NotFound("Game session not found.");

            if (!session.State.IsRunning)
                throw ApiException.Conflict("Match has finished.");

            var paddle = connection.Side == GamePlayerSide.Host
                ? session.State.HostPaddle
                : session.State.GuestPaddle;

            paddle.Y = Math.Clamp(y, PaddleHalfHeight, 1 - PaddleHalfHeight);
            Touch(roomCode, connection.Side);

            return Snapshot(session.State);
        }
    }

    public IReadOnlyList<GameStateDto> TickAll()
    {
        lock (sync)
        {
            foreach (var session in sessions.Values)
            {
                foreach (var connection in connections.Values
                    .Where(c => c.RoomCode == session.State.RoomCode))
                    Touch(connection.RoomCode, connection.Side);

                var gameState = session.State;

                if (!gameState.IsRunning)
                    continue;

                var previousX = gameState.Ball.X;

                MoveBall(gameState);
                BounceFromTopBottom(gameState);
                HandlePaddleCollisionOrScore(gameState, previousX);

                if (gameState.HostScore >= gameState.TargetScore ||
                    gameState.GuestScore >= gameState.TargetScore)
                {
                    gameState.IsRunning = false;
                }
            }

            // Retry completed states until the result has been durably persisted.
            return sessions.Values
                .Select(s => Snapshot(s.State))
                .ToArray();
        }
    }

    public IReadOnlyList<string> GetAbandonedRooms()
    {
        lock (sync)
        {
            var cutoff = clock.GetUtcNow().AddSeconds(-options.Value.ReconnectGraceSeconds);

            return sessions
                .Where(s => s.Value.State.IsRunning &&
                    (s.Value.HostLastSeen < cutoff || s.Value.GuestLastSeen < cutoff))
                .Select(s => s.Key)
                .ToArray();
        }
    }

    private void Touch(string code, GamePlayerSide? side)
    {
        if (!sessions.TryGetValue(code, out var session))
            return;

        if (side == GamePlayerSide.Host)
            session.HostLastSeen = clock.GetUtcNow();

        if (side == GamePlayerSide.Guest)
            session.GuestLastSeen = clock.GetUtcNow();
    }

    private static void MoveBall(GameStateDto gameState)
    {
        gameState.Ball.X += gameState.Ball.VelocityX;
        gameState.Ball.Y += gameState.Ball.VelocityY;
    }

    private static void BounceFromTopBottom(GameStateDto gameState)
    {
        var ball = gameState.Ball;

        if (ball.Y <= 0 || ball.Y >= 1)
        {
            ball.VelocityY = ball.Y <= 0
                ? Math.Abs(ball.VelocityY)
                : -Math.Abs(ball.VelocityY);

            ball.Y = Math.Clamp(ball.Y, 0, 1);
        }
    }

    private static void HandlePaddleCollisionOrScore(GameStateDto gameState, double previousX)
    {
        var ball = gameState.Ball;

        if (ball.VelocityX < 0 && previousX > HostPaddleX && ball.X <= HostPaddleX &&
            IsPaddleHit(ball.Y, gameState.HostPaddle.Y))
        {
            ball.X = HostPaddleX;
            ball.VelocityX = Math.Abs(ball.VelocityX);
        }
        else if (ball.VelocityX > 0 && previousX < GuestPaddleX && ball.X >= GuestPaddleX &&
            IsPaddleHit(ball.Y, gameState.GuestPaddle.Y))
        {
            ball.X = GuestPaddleX;
            ball.VelocityX = -Math.Abs(ball.VelocityX);
        }

        if (ball.X <= 0 || ball.X >= 1)
        {
            if (ball.X <= 0)
                gameState.GuestScore++;
            else
                gameState.HostScore++;

            var direction = ball.X <= 0 ? 1 : -1;

            ResetBall(gameState, direction);
        }
    }

    private static bool IsPaddleHit(double ballY, double paddleY)
    {
        return Math.Abs(ballY - paddleY) <= PaddleHalfHeight;
    }

    private static void ResetBall(GameStateDto gameState, int directionX)
    {
        gameState.Ball = new()
        {
            X = 0.5,
            Y = 0.5,
            VelocityX = directionX * 0.01,
            VelocityY = 0.01
        };
    }

    private static GameStateDto Snapshot(GameStateDto gameState)
    {
        return new()
        {
            RoomCode = gameState.RoomCode,
            IsRunning = gameState.IsRunning,
            TargetScore = gameState.TargetScore,
            HostScore = gameState.HostScore,
            GuestScore = gameState.GuestScore,
            HostPaddle = new()
            {
                Y = gameState.HostPaddle.Y
            },
            GuestPaddle = new()
            {
                Y = gameState.GuestPaddle.Y
            },
            Ball = new()
            {
                X = gameState.Ball.X,
                Y = gameState.Ball.Y,
                VelocityX = gameState.Ball.VelocityX,
                VelocityY = gameState.Ball.VelocityY
            }
        };
    }
}
