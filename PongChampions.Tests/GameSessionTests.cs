using Microsoft.Extensions.Options;
using PongChampions.Api.Common;
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

public sealed class GameSessionTests
{
    private readonly TestClock clock = new();
    private readonly Guid host = Guid.NewGuid();
    private readonly Guid guest = Guid.NewGuid();

    private GameSessionService NewSession()
    {
        return new(clock, Options.Create(new GameOptions()));
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
    public void BallBouncesFromPaddleAndWalls()
    {
        var game = NewSession();

        game.CreateSession("ABC123", 5);
        game.RegisterConnection("guest", "ABC123", guest, host, guest);
        game.UpdatePaddle("guest", "ABC123", 0.88);

        var states = Enumerable.Range(0, 55)
            .Select(_ => game.TickAll()
            .Single())
            .ToArray();

        Assert.Contains(states, s => s.Ball.VelocityX < 0);
        Assert.Contains(states, s => s.Ball.VelocityY < 0);
        Assert.All(states, s => Assert.InRange(s.Ball.Y, 0, 1));
        Assert.Equal(0, states.Last().HostScore + states.Last().GuestScore);
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
