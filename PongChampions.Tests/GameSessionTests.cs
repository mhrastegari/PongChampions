using Microsoft.Extensions.Options;
using PongChampions.Api.Common;
using PongChampions.Api.Common.Dtos.Game;
using PongChampions.Api.Services;

namespace PongChampions.Tests;

public sealed class TestClock : TimeProvider
{
    private DateTimeOffset now = DateTimeOffset.UtcNow;

    public override DateTimeOffset GetUtcNow()
    {
        return now;
    }

    public void Advance(TimeSpan duration)
    {
        now += duration;
    }
}

public sealed class FixedRandom(double value) : Random
{
    public override double NextDouble()
    {
        return value;
    }
}

public sealed class GameSessionTests
{
    private readonly TestClock clock = new();
    private readonly Guid host = Guid.NewGuid();
    private readonly Guid guest = Guid.NewGuid();

    private GameSessionService NewSession(
        int serveDelayMilliseconds = 0,
        int tickMilliseconds = 50,
        Random? random = null)
    {
        return new(
            clock,
            Options.Create(new GameOptions
            {
                TickMilliseconds = tickMilliseconds,
                ServeDelayMilliseconds = serveDelayMilliseconds
            }),
            random ?? new Random(1234));
    }

    private static double Speed(GameStateDto state)
    {
        return Math.Sqrt(state.Ball.VelocityX * state.Ball.VelocityX + state.Ball.VelocityY * state.Ball.VelocityY);
    }

    // Ticks until the ball nears the guest paddle, then predicts where its center will touch the paddle face.
    private static double PredictGuestContactY(GameSessionService game)
    {
        var state = game.TickAll()
            .Single();

        while (state.Ball.X < 0.8)
            state = game.TickAll()
                .Single();

        return state.Ball.Y + (0.935 - state.Ball.X) * state.Ball.VelocityY / state.Ball.VelocityX;
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void InvalidCoordinatesCannotEnterState(double y)
    {
        var game = NewSession();

        game.CreateSession("ABC123", 1);
        game.RegisterConnection("host", "ABC123", host, host, guest);
        Assert.Equal(400, Assert.Throws<ApiException>(() => game.UpdatePaddle("host", "ABC123", y)).StatusCode);
        Assert.Equal(0.5, game.GetSession("ABC123")!.HostPaddle.Y);
    }

    [Fact]
    public void OnlyParticipantCanControlOwnPaddleInJoinedRoom()
    {
        var game = NewSession();

        game.CreateSession("ABC123", 1);
        game.RegisterConnection("spectator", "ABC123", Guid.NewGuid(), host, guest);
        Assert.Equal(
            403,
            Assert.Throws<ApiException>(() => game.UpdatePaddle("spectator", "ABC123", 0.2)).StatusCode);
        game.RegisterConnection("guest", "ABC123", guest, host, guest);

        var state = game.UpdatePaddle("guest", "ABC123", 0.2);

        Assert.Equal(0.2, state.GuestPaddle.Y);
        Assert.Equal(0.5, state.HostPaddle.Y);
        Assert.Equal(403, Assert.Throws<ApiException>(() => game.UpdatePaddle("guest", "OTHER1", 0.2)).StatusCode);
        game.RevokePlayer("ABC123", guest);
        Assert.Equal(403, Assert.Throws<ApiException>(() => game.UpdatePaddle("guest", "ABC123", 0.2)).StatusCode);
    }

    [Fact]
    public void ReturnedStateCannotMutateServerAndFinishedStateIsRetried()
    {
        var game = NewSession();
        var initial = game.CreateSession("ABC123", 1);

        initial.Ball.X = 999;
        Assert.Equal(0.5, game.GetSession("ABC123")!.Ball.X);

        var states = Enumerable.Range(0, 500)
            .Select(_ => game.TickAll()
            .Single())
            .ToArray();
        var final = states.Last();

        Assert.False(final.IsRunning);
        Assert.Equal(1, final.HostScore + final.GuestScore);
        Assert.Equal(final.HostScore, game.TickAll()
            .Single().HostScore);
        game.RemoveSession("ABC123");
        Assert.Empty(game.TickAll());
    }

    [Fact]
    public void ServeWaitsAtCenterBeforeLaunching()
    {
        var game = NewSession(serveDelayMilliseconds: 1000);
        var initial = game.CreateSession("ABC123", 5);

        Assert.Equal((0.5, 0.5, 0.0), (initial.Ball.X, initial.Ball.Y, Speed(initial)));

        var waiting = Enumerable.Range(0, 19)
            .Select(_ => game.TickAll()
            .Single())
            .ToArray();

        Assert.All(waiting, s => Assert.Equal((0.5, 0.5, 0.0), (s.Ball.X, s.Ball.Y, Speed(s))));

        var launched = game.TickAll()
            .Single();

        Assert.True(launched.Ball.VelocityX > 0);
        Assert.True(launched.Ball.X > 0.5);
        Assert.Equal(0.45, Speed(launched), 9);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.25)]
    [InlineData(0.75)]
    [InlineData(0.999)]
    public void ServeAngleIsNeverFlatOrTooSteep(double roll)
    {
        var game = NewSession(random: new FixedRandom(roll));
        var state = game.CreateSession("ABC123", 5);
        var angle = Math.Abs(Math.Atan2(state.Ball.VelocityY, state.Ball.VelocityX)) * 180 / Math.PI;

        Assert.InRange(angle, 15 - 1e-9, 35 + 1e-9);
    }

    [Fact]
    public void BallMotionDoesNotDependOnTickRate()
    {
        var coarse = NewSession(tickMilliseconds: 50, random: new FixedRandom(0.6));
        var fine = NewSession(tickMilliseconds: 25, random: new FixedRandom(0.6));

        coarse.CreateSession("ABC123", 5);
        fine.CreateSession("ABC123", 5);

        var coarseState = Enumerable.Range(0, 10)
            .Select(_ => coarse.TickAll()
            .Single())
            .ToArray()
            .Last();
        var fineState = Enumerable.Range(0, 20)
            .Select(_ => fine.TickAll()
            .Single())
            .ToArray()
            .Last();

        Assert.True(coarseState.Ball.X > 0.65);
        Assert.Equal(coarseState.Ball.X, fineState.Ball.X, 9);
        Assert.Equal(coarseState.Ball.Y, fineState.Ball.Y, 9);
    }

    [Theory]
    [InlineData(-0.06, 1)]
    [InlineData(0.06, -1)]
    public void PaddleContactPointSetsReturnAngleAndRallySpeedsUp(double paddleOffset, int expectedVerticalDirection)
    {
        var game = NewSession(random: new FixedRandom(0.9));

        game.CreateSession("ABC123", 5);
        game.RegisterConnection("guest", "ABC123", guest, host, guest);
        game.UpdatePaddle("guest", "ABC123", PredictGuestContactY(game) + paddleOffset);

        var returned = Enumerable.Range(0, 10)
            .Select(_ => game.TickAll()
            .Single())
            .First(s => s.Ball.VelocityX < 0);

        Assert.Equal(0, returned.HostScore + returned.GuestScore);
        Assert.Equal(expectedVerticalDirection, Math.Sign(returned.Ball.VelocityY));
        Assert.Equal(0.45 * 1.07, Speed(returned), 9);
    }

    [Fact]
    public void CenterHitReturnsFlatterThanEdgeHit()
    {
        Assert.True(GuestReturnAngle(-0.01) < GuestReturnAngle(-0.11));
    }

    [Fact]
    public void MissedBallScoresAndServesTowardScorer()
    {
        var game = NewSession(random: new FixedRandom(0.9));

        game.CreateSession("ABC123", 5);
        game.RegisterConnection("guest", "ABC123", guest, host, guest);
        game.UpdatePaddle("guest", "ABC123", PredictGuestContactY(game) > 0.5 ? 0.12 : 0.88);

        var scored = Enumerable.Range(0, 40)
            .Select(_ => game.TickAll()
            .Single())
            .First(s => s.HostScore == 1);

        Assert.Equal(0, scored.GuestScore);
        Assert.Equal((0.5, 0.5), (scored.Ball.X, scored.Ball.Y));
        Assert.True(scored.Ball.VelocityX < 0);
    }

    [Fact]
    public void LongRalliesStayOnBoardAndRespectSpeedCap()
    {
        var game = NewSession();

        game.CreateSession("ABC123", 1000);
        game.RegisterConnection("host", "ABC123", host, host, guest);
        game.RegisterConnection("guest", "ABC123", guest, host, guest);

        var returns = 0;
        var previous = game.GetSession("ABC123")!;

        for (var i = 0; i < 4000; i++)
        {
            // Both players track the ball, so the rally should never end.
            game.UpdatePaddle("host", "ABC123", previous.Ball.Y);
            game.UpdatePaddle("guest", "ABC123", previous.Ball.Y);

            var state = game.TickAll()
                .Single();

            Assert.InRange(state.Ball.X, 0, 1);
            Assert.InRange(state.Ball.Y, 0.015, 0.985);
            Assert.True(Speed(state) <= 1.2 + 1e-9);

            if (Math.Sign(state.Ball.VelocityX) != Math.Sign(previous.Ball.VelocityX))
                returns++;

            previous = state;
        }

        Assert.Equal(0, previous.HostScore + previous.GuestScore);
        Assert.True(returns > 20);
        Assert.Equal(1.2, Speed(previous), 9);
    }

    [Fact]
    public void FinishedMatchStopsBall()
    {
        var game = NewSession();

        game.CreateSession("ABC123", 1);
        game.RegisterConnection("host", "ABC123", host, host, guest);

        var final = Enumerable.Range(0, 200)
            .Select(_ => game.TickAll()
            .Single())
            .First(s => !s.IsRunning);

        Assert.Equal((0.5, 0.5, 0.0), (final.Ball.X, final.Ball.Y, Speed(final)));
        Assert.Equal(final.Ball.X, game.TickAll()
            .Single().Ball.X);
        Assert.Equal(409, Assert.Throws<ApiException>(() => game.UpdatePaddle("host", "ABC123", 0.3)).StatusCode);
    }

    private double GuestReturnAngle(double paddleOffset)
    {
        var game = NewSession(random: new FixedRandom(0.9));

        game.CreateSession("ABC123", 5);
        game.RegisterConnection("guest", "ABC123", guest, host, guest);
        game.UpdatePaddle("guest", "ABC123", PredictGuestContactY(game) + paddleOffset);

        var returned = Enumerable.Range(0, 10)
            .Select(_ => game.TickAll()
            .Single())
            .First(s => s.Ball.VelocityX < 0);

        return Math.Abs(Math.Atan2(returned.Ball.VelocityY, -returned.Ball.VelocityX));
    }

    [Fact]
    public void ReconnectionRestoresControlAndGracePeriodIsEnforced()
    {
        var game = NewSession();

        game.CreateSession("ABC123", 5);
        game.RegisterConnection("h1", "ABC123", host, host, guest);
        game.RegisterConnection("g1", "ABC123", guest, host, guest);
        game.RemoveConnection("g1");
        clock.Advance(TimeSpan.FromSeconds(59));
        game.TickAll();
        Assert.Empty(game.GetAbandonedRooms());
        game.RegisterConnection("g2", "ABC123", guest, host, guest);
        Assert.Equal(0.3, game.UpdatePaddle("g2", "ABC123", 0.3).GuestPaddle.Y);
        game.RemoveConnection("g2");
        clock.Advance(TimeSpan.FromSeconds(61));
        game.TickAll();
        Assert.Contains("ABC123", game.GetAbandonedRooms());
        game.Clear();
        Assert.Null(game.GetSession("ABC123"));
    }
}
