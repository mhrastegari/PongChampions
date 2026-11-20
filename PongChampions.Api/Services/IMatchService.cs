using PongChampions.Api.Common.Dtos.Game;

namespace PongChampions.Api.Services;

public interface IMatchService
{
    Task<bool> FinishMatchAsync(
        GameStateDto gameState,
        CancellationToken cancellationToken = default);
}