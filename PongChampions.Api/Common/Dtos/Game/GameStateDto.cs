namespace PongChampions.Api.Common.Dtos.Game;

public class GameStateDto
{
    public required string RoomCode { get; set; }

    public PaddleStateDto HostPaddle { get; set; } = new();
    public PaddleStateDto GuestPaddle { get; set; } = new();

    public BallStateDto Ball { get; set; } = new();

    public int HostScore { get; set; }
    public int GuestScore { get; set; }

    public bool IsRunning { get; set; }
}
