namespace Epsilon.Games;

public interface IBattleBallLifecycleService
{
    ValueTask<GameSessionUpdateResult> PrepareMatchAsync(
        string sessionKey,
        CancellationToken cancellationToken = default);

    ValueTask<GameSessionUpdateResult> StartRoundAsync(
        string sessionKey,
        CancellationToken cancellationToken = default);

    ValueTask<GameSessionUpdateResult> AwardPointsAsync(
        string sessionKey,
        string teamKey,
        int points,
        CancellationToken cancellationToken = default);

    ValueTask<GameSessionUpdateResult> FinishMatchAsync(
        string sessionKey,
        CancellationToken cancellationToken = default);
}
