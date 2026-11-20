using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PongChampions.Api.Common.Dtos.Game;
using PongChampions.Api.Common.Dtos.User;
using PongChampions.Api.Services;
using PongChampions.Data.Enums;
using System.Net;
using System.Net.Http.Json;

namespace PongChampions.Tests;

[Collection("backend")]
public sealed class ApiAndHubTests(BackendFixture fixture)
{
    [Fact]
    public async Task UnexpectedFailuresAreSanitizedAndAuthenticationRequestsAreRateLimited()
    {
        await using var factory = fixture.Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimits:AuthPerMinute"] = "1"
            }));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAuthService>();
                services.AddScoped<IAuthService, FailingAuth>();
            });
        });

        using var client = factory.CreateClient();

        var payload = new
        {
            username = "validuser",
            password = "TestPassword_123",
            displayName = "Test"
        };

        var failed = await client.PostAsJsonAsync("/api/auth/register", payload);

        Assert.Equal(HttpStatusCode.InternalServerError, failed.StatusCode);

        var body = await failed.Content.ReadAsStringAsync();

        Assert.DoesNotContain("private-database-detail", body);
        Assert.Contains("traceId", body);

        var throttled = await client.PostAsJsonAsync("/api/auth/register", payload);

        Assert.Equal(HttpStatusCode.TooManyRequests, throttled.StatusCode);
        Assert.Equal("application/problem+json", throttled.Content.Headers.ContentType?.MediaType);
    }

    private sealed class FailingAuth : IAuthService
    {
        public Task RegisterAsync(UserRegisterDto dto)
        {
            throw new IOException("private-database-detail");
        }

        public Task<AuthResponseDto?> LoginAsync(UserLoginDto dto)
        {
            throw new IOException("private-database-detail");
        }
    }

    [Fact]
    public async Task EndToEndMatchBroadcastsWinnerAndPersistsOneResult()
    {
        var token = await fixture.RegisterAsync();
        var room = await fixture.CreateRoomAsync(token);

        // Shorten the fixture's match without expanding the public room-creation API.
        await using (var setup = fixture.NewContext())
            await setup.Rooms
                .Where(r => r.Id == room.Id).ExecuteUpdateAsync(s => s.SetProperty(r => r.MaxScore, 1));

        var guest = await fixture.JoinAsync(room.Code);
        await using var hostHub = fixture.Hub(token);
        await using var guestHub = fixture.Hub(guest.GuestToken);

        var finished = new TaskCompletionSource<GameStateDto>(TaskCreationOptions.RunContinuationsAsynchronously);

        hostHub.On<GameStateDto>("MatchFinished", state => finished.TrySetResult(state));
        await Task.WhenAll(hostHub.StartAsync(), guestHub.StartAsync());
        await hostHub.InvokeAsync("JoinRoom", room.Code);
        await guestHub.InvokeAsync("JoinRoom", room.Code);

        using var hostClient = fixture.Client(token);

        (await hostClient.PostAsync($"/api/rooms/{room.Code}/start", null)).EnsureSuccessStatusCode();

        var final = await finished.Task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.False(final.IsRunning);
        Assert.Equal(1, final.HostScore + final.GuestScore);

        await using var db = fixture.NewContext();
        var match = Assert.Single(await db.Matches
            .Where(m => m.RoomId == room.Id)
            .ToListAsync());

        Assert.Equal(final.HostScore, match.Player1Score);
        Assert.Equal(final.GuestScore, match.Player2Score);
        Assert.Equal(
            final.HostScore > final.GuestScore
                ? room.HostPlayerId
                : guest.Room.GuestPlayerId,
            match.WinnerPlayerId);
        Assert.Equal(RoomStatus.Finished, (await db.Rooms
            .SingleAsync(r => r.Id == room.Id)).Status);
    }

    [Fact]
    public async Task HttpResponsesDistinguishValidationAuthenticationPermissionMissingAndConflict()
    {
        using var anonymous = fixture.Client();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsync("/api/rooms", null)).StatusCode);

        var missing = await anonymous.GetAsync("/api/rooms/ZZZZZZ");

        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal("application/problem+json", missing.Content.Headers.ContentType?.MediaType);
        Assert.Equal(HttpStatusCode.BadRequest, (await anonymous.GetAsync("/api/rooms/invalid")).StatusCode);

        var host = await fixture.RegisterAsync();
        var room = await fixture.CreateRoomAsync(host);
        var guest = await fixture.JoinAsync(room.Code);

        Assert.NotNull(guest.GuestToken);
        Assert.Equal(
            HttpStatusCode.Conflict,
            (await anonymous.PostAsync($"/api/rooms/{room.Code}/join", null)).StatusCode);

        using var guestClient = fixture.Client(guest.GuestToken);

        Assert.Equal(HttpStatusCode.Forbidden, (await guestClient.PostAsync("/api/rooms", null)).StatusCode);

        using var other = fixture.Client(await fixture.RegisterAsync());

        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await other.PostAsync($"/api/rooms/{room.Code}/start", null)).StatusCode);

        using var invalid = fixture.Client(host[..^5] + "bogus");

        Assert.Equal(HttpStatusCode.Unauthorized, (await invalid.GetAsync($"/api/rooms/{room.Code}")).StatusCode);
    }

    [Fact]
    public async Task InvalidRegistrationAndDuplicateUsernameUseProblemDetails()
    {
        using var client = fixture.Client();

        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync("/api/auth/register", new { username = "x", password = "short", displayName = "" })).StatusCode);

        var credentials = new
        {
            username = "u" + Guid.NewGuid().ToString("N")[..20],
            password = "TestPassword_123",
            displayName = "Player"
        };

        (await client.PostAsJsonAsync("/api/auth/register", credentials)).EnsureSuccessStatusCode();
        Assert.Equal(
            HttpStatusCode.Conflict,
            (await client.PostAsJsonAsync("/api/auth/register", credentials)).StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/api/auth/login", new { credentials.username, password = "wrongpassword" })).StatusCode);
    }

    [Fact]
    public async Task ConcurrentGuestJoinsReserveExactlyOneSlot()
    {
        var room = await fixture.CreateRoomAsync(await fixture.RegisterAsync());
        using var a = fixture.Client();
        using var b = fixture.Client();

        var responses = await Task.WhenAll(
            a.PostAsync($"/api/rooms/{room.Code}/join", null),
            b.PostAsync($"/api/rooms/{room.Code}/join", null));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);

        await using var db = fixture.NewContext();
        var saved = await db.Rooms
            .SingleAsync(r => r.Id == room.Id);

        Assert.NotNull(saved.GuestPlayerId);
        Assert.Equal(RoomStatus.Ready, saved.Status);
    }

    [Fact]
    public async Task RegisteredPlayerCannotJoinTwoRoomsConcurrently()
    {
        var a = await fixture.CreateRoomAsync(await fixture.RegisterAsync());
        var b = await fixture.CreateRoomAsync(await fixture.RegisterAsync());
        var token = await fixture.RegisterAsync();
        using var first = fixture.Client(token);
        using var second = fixture.Client(token);

        var responses = await Task.WhenAll(
            first.PostAsync($"/api/rooms/{a.Code}/join", null),
            second.PostAsync($"/api/rooms/{b.Code}/join", null));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task SignalRUsesVerifiedIdentityRejectsSpectatorAndRestoresStateOnReconnect()
    {
        var token = await fixture.RegisterAsync();
        var room = await fixture.CreateRoomAsync(token);
        var guest = await fixture.JoinAsync(room.Code);
        await using var hostHub = fixture.Hub(token);
        await using var guestHub = fixture.Hub(guest.GuestToken);
        await using var spectator = fixture.Hub();

        await Task.WhenAll(hostHub.StartAsync(), guestHub.StartAsync(), spectator.StartAsync());
        await hostHub.InvokeAsync("JoinRoom", room.Code);
        await guestHub.InvokeAsync("JoinRoom", room.Code);
        await spectator.InvokeAsync("JoinRoom", room.Code);
        // Supplying the host GUID is no longer a valid hub contract.
        await Assert.ThrowsAsync<HubException>(() => spectator.InvokeAsync("JoinRoom", room.Code, room.HostPlayerId));

        using var hostClient = fixture.Client(token);

        (await hostClient.PostAsync($"/api/rooms/{room.Code}/start", null)).EnsureSuccessStatusCode();
        await Assert.ThrowsAsync<HubException>(() => spectator.InvokeAsync("MovePaddle", room.Code, 0.2));
        await hostHub.InvokeAsync("MovePaddle", room.Code, 0.2);
        await guestHub.InvokeAsync("MovePaddle", room.Code, 0.7);

        var sessions = fixture.Factory.Services
            .GetRequiredService<IGameSessionService>();

        Assert.Equal(0.2, sessions.GetSession(room.Code)!.HostPaddle.Y);
        Assert.Equal(0.7, sessions.GetSession(room.Code)!.GuestPaddle.Y);
        await guestHub.StopAsync();

        await using var reconnected = fixture.Hub(guest.GuestToken);

        var stateReceived = new TaskCompletionSource<GameStateDto>(TaskCreationOptions.RunContinuationsAsynchronously);

        reconnected.On<GameStateDto>("GameStateUpdated", state => stateReceived.TrySetResult(state));
        await reconnected.StartAsync();
        await reconnected.InvokeAsync("JoinRoom", room.Code);
        Assert.Equal(room.Code, (await stateReceived.Task.WaitAsync(TimeSpan.FromSeconds(5))).RoomCode);
        await reconnected.InvokeAsync("MovePaddle", room.Code, 0.4);
        Assert.Equal(0.4, sessions.GetSession(room.Code)!.GuestPaddle.Y);
        (await hostClient.PostAsync($"/api/rooms/{room.Code}/close", null)).EnsureSuccessStatusCode();
        Assert.Null(sessions.GetSession(room.Code));

        await using var db = fixture.NewContext();

        Assert.Equal(RoomStatus.Cancelled, (await db.Rooms
            .SingleAsync(r => r.Id == room.Id)).Status);
        Assert.False(await db.Matches
            .AnyAsync(m => m.RoomId == room.Id));
    }

    [Fact]
    public async Task GuestCredentialIsRoomScopedAndRevokedWhenGuestLeaves()
    {
        var room = await fixture.CreateRoomAsync(await fixture.RegisterAsync());
        var other = await fixture.CreateRoomAsync(await fixture.RegisterAsync());
        var guest = await fixture.JoinAsync(room.Code);
        await using var hub = fixture.Hub(guest.GuestToken);

        await hub.StartAsync();
        await Assert.ThrowsAsync<HubException>(() => hub.InvokeAsync("JoinRoom", other.Code));
        await hub.InvokeAsync("JoinRoom", room.Code);

        using var client = fixture.Client(guest.GuestToken);

        (await client.PostAsync($"/api/rooms/{room.Code}/leave", null)).EnsureSuccessStatusCode();
        await fixture.JoinAsync(room.Code);
        await Assert.ThrowsAsync<HubException>(() => hub.InvokeAsync("JoinRoom", room.Code));
        await Assert.ThrowsAsync<HubException>(() => hub.InvokeAsync("MovePaddle", room.Code, 0.2));
    }

    [Fact]
    public async Task RegisteredGuestGetsControlThroughAccountJwt()
    {
        var hostToken = await fixture.RegisterAsync();
        var room = await fixture.CreateRoomAsync(hostToken);
        var guestToken = await fixture.RegisterAsync();

        Assert.Null((await fixture.JoinAsync(room.Code, guestToken)).GuestToken);

        await using var hub = fixture.Hub(guestToken);

        await hub.StartAsync();
        await hub.InvokeAsync("JoinRoom", room.Code);

        using var host = fixture.Client(hostToken);

        (await host.PostAsync($"/api/rooms/{room.Code}/start", null)).EnsureSuccessStatusCode();
        await hub.InvokeAsync("MovePaddle", room.Code, 0.3);
        Assert.Equal(
            0.3,
            fixture.Factory.Services
                .GetRequiredService<IGameSessionService>().GetSession(room.Code)!.GuestPaddle.Y);
        (await host.PostAsync($"/api/rooms/{room.Code}/close", null)).EnsureSuccessStatusCode();
    }
}
