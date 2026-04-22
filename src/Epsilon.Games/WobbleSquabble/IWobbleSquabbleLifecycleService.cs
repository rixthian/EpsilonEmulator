namespace Epsilon.Games;

public interface IWobbleSquabbleLifecycleService
{
    ValueTask<GameSessionUpdateResult> PrepareDuelAsync(
        string sessionKey,
        CancellationToken cancellationToken = default);

    ValueTask<GameSessionUpdateResult> StartDuelAsync(
        string sessionKey,
        CancellationToken cancellationToken = default);

    ValueTask<GameSessionUpdateResult> AwardPointsAsync(
        string sessionKey,
        string teamKey,
        int points,
        CancellationToken cancellationToken = default);

    ValueTask<GameSessionUpdateResult> FinishDuelAsync(
        string sessionKey,
        CancellationToken cancellationToken = default);
}
