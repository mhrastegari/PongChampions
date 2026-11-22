namespace PongChampions.Api.Services;

public sealed class GameOptions
{
    public int TickMilliseconds { get; set; } = 50;
    public int ReconnectGraceSeconds { get; set; } = 60;
    public int LobbyLifetimeMinutes { get; set; } = 30;
    public int ServeDelayMilliseconds { get; set; } = 1000;
}
