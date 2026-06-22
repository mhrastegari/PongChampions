namespace PongChampions.Api.Common.Dtos;

public abstract class BaseDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
}
