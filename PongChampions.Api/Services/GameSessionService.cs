using Microsoft.Extensions.Options;
using PongChampions.Api.Common;
using PongChampions.Api.Common.Dtos.Game;
using PongChampions.Api.Common.Enums;

namespace PongChampions.Api.Services;

public sealed class GameSessionService : IGameSessionService
{
    // Board coordinates are normalized to [0, 1] on both axes. Speeds are board units per second.
    private const double HostPaddleX = 0.05;
    private const double GuestPaddleX = 0.95;
    private const double PaddleHalfHeight = 0.12;
    private const double BallRadius = 0.015;

    private const double ServeSpeed = 0.45;
    private const double MaxBallSpeed = 1.2;
    private const double RallySpeedup = 1.07;

    private const double MinServeAngle = Math.PI / 12;
    private const double MaxServeAngle = Math.PI * 7 / 36;
    private const double MaxBounceAngle = Math.PI * 5 / 18;

    // Sub-step movement so a fast ball can never skip over a paddle or a wall.
    private const double MaxStepDistance = 0.005;

    private readonly TimeProvider clock;
    private readonly IOptions<GameOptions> options;
    private readonly Random random;
    private readonly double tickSeconds;
    private readonly int serveDelayTicks;

    private readonly object sync = new();

    private readonly Dictionary<string, Session> sessions = new();

    private readonly Dictionary<string, PlayerConnection> connections = new();

    public GameSessionService(TimeProvider clock, IOptions<GameOptions> options)
        : this(clock, options, Random.Shared)
    {
    }

    public GameSessionService(TimeProvider clock, IOptions<GameOptions> options, Random random)
    {
        this.clock = clock;
        this.options = options;
        this.random = random;

        var tickMilliseconds = options.Value.TickMilliseconds;

        tickSeconds = tickMilliseconds / 1000.0;
        serveDelayTicks = (int)Math.Ceiling((double)options.Value.ServeDelayMilliseconds / tickMilliseconds);
    }

    private sealed class Session(
        GameStateDto gameState,
        DateTimeOffset now)
    {
        public GameStateDto State { get; } = gameState;
        public DateTimeOffset HostLastSeen { get; set; } = now;
        public DateTimeOffset GuestLastSeen { get; set; } = now;

        // While positive, the ball waits at the center and the pending serve velocity is withheld.
        public int ServeTicksRemaining { get; set; }
        public double ServeVelocityX { get; set; }
        public double ServeVelocityY { get; set; }
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
                }
            };

            var session = new Session(gameState, clock.GetUtcNow());

            Serve(session, 1);
            sessions.Add(roomCode, session);

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
            foreach (var connection in connections.Values)
                Touch(connection.RoomCode, connection.Side);

            foreach (var session in sessions.Values)
            {
                if (!session.State.IsRunning)
                    continue;

                if (session.ServeTicksRemaining > 0)
                {
                    session.ServeTicksRemaining--;

                    if (session.ServeTicksRemaining > 0)
                        continue;

                    Launch(session);
                }

                Simulate(session, tickSeconds);
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

    private void Simulate(Session session, double seconds)
    {
        var ball = session.State.Ball;
        var distance = Speed(ball) * seconds;
        var steps = Math.Max(1, (int)Math.Ceiling(distance / MaxStepDistance));
        var step = seconds / steps;

        for (var i = 0; i < steps; i++)
        {
            var previousX = ball.X;
            var previousY = ball.Y;

            ball.X += ball.VelocityX * step;
            ball.Y += ball.VelocityY * step;

            BounceFromTopBottom(ball);

            if (TryPaddleHit(ball, previousX, previousY, session.State.HostPaddle.Y, HostPaddleX + BallRadius, 1) ||
                TryPaddleHit(ball, previousX, previousY, session.State.GuestPaddle.Y, GuestPaddleX - BallRadius, -1))
                continue;

            if (ball.X > 0 && ball.X < 1)
                continue;

            Score(session, ball.X <= 0 ? GamePlayerSide.Guest : GamePlayerSide.Host);

            return;
        }
    }

    private static void BounceFromTopBottom(BallStateDto ball)
    {
        // Reflect any overshoot so the ball keeps its full travel distance.
        if (ball.Y < BallRadius)
        {
            ball.Y = 2 * BallRadius - ball.Y;
            ball.VelocityY = Math.Abs(ball.VelocityY);
        }
        else if (ball.Y > 1 - BallRadius)
        {
            ball.Y = 2 * (1 - BallRadius) - ball.Y;
            ball.VelocityY = -Math.Abs(ball.VelocityY);
        }
    }

    // contactX is where the ball center sits when its edge touches the paddle face.
    // returnDirection is the horizontal direction the ball leaves the paddle in.
    private static bool TryPaddleHit(
        BallStateDto ball,
        double previousX,
        double previousY,
        double paddleY,
        double contactX,
        int returnDirection)
    {
        var approaching = returnDirection > 0
            ? ball.VelocityX < 0 && previousX > contactX && ball.X <= contactX
            : ball.VelocityX > 0 && previousX < contactX && ball.X >= contactX;

        if (!approaching)
            return false;

        var fraction = (previousX - contactX) / (previousX - ball.X);
        var contactY = previousY + (ball.Y - previousY) * fraction;
        var reach = PaddleHalfHeight + BallRadius;
        var offset = (contactY - paddleY) / reach;

        if (Math.Abs(offset) > 1)
            return false;

        // Edge hits leave at a steep angle; center hits return nearly flat.
        var angle = offset * MaxBounceAngle;
        var speed = Math.Min(Speed(ball) * RallySpeedup, MaxBallSpeed);

        ball.X = contactX;
        ball.Y = Math.Clamp(contactY, BallRadius, 1 - BallRadius);
        ball.VelocityX = returnDirection * speed * Math.Cos(angle);
        ball.VelocityY = speed * Math.Sin(angle);

        return true;
    }

    private void Score(Session session, GamePlayerSide scorer)
    {
        var gameState = session.State;

        if (scorer == GamePlayerSide.Host)
            gameState.HostScore++;
        else
            gameState.GuestScore++;

        if (gameState.HostScore >= gameState.TargetScore ||
            gameState.GuestScore >= gameState.TargetScore)
        {
            gameState.IsRunning = false;
            gameState.Ball = new()
            {
                X = 0.5,
                Y = 0.5
            };

            return;
        }

        // Serve away from the player who conceded so they have time to recover.
        Serve(session, scorer == GamePlayerSide.Host ? -1 : 1);
    }

    private void Serve(Session session, int directionX)
    {
        var angle = MinServeAngle + random.NextDouble() * (MaxServeAngle - MinServeAngle);

        if (random.NextDouble() < 0.5)
            angle = -angle;

        session.State.Ball = new()
        {
            X = 0.5,
            Y = 0.5
        };
        session.ServeVelocityX = directionX * ServeSpeed * Math.Cos(angle);
        session.ServeVelocityY = ServeSpeed * Math.Sin(angle);
        session.ServeTicksRemaining = serveDelayTicks;

        if (serveDelayTicks == 0)
            Launch(session);
    }

    private static void Launch(Session session)
    {
        session.State.Ball.VelocityX = session.ServeVelocityX;
        session.State.Ball.VelocityY = session.ServeVelocityY;
    }

    private static double Speed(BallStateDto ball)
    {
        return Math.Sqrt(ball.VelocityX * ball.VelocityX + ball.VelocityY * ball.VelocityY);
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
