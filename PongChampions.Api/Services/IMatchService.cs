using PongChampions.Api.Common.Dtos.Game;

namespace PongChampions.Api.Services;

public interface IMatchService
{
    Task<MatchCompletionResult> FinishMatchAsync(GameStateDto gameState, CancellationToken cancellationToken = default);
}
