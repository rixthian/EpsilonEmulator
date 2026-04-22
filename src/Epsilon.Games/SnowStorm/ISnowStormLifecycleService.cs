namespace Epsilon.Games;

public interface ISnowStormLifecycleService
{
    ValueTask<GameSessionUpdateResult> PrepareMatchAsync(
        string sessionKey,
        CancellationToken cancellationToken = default);

    ValueTask<GameSessionUpdateResult> StartBattleAsync(
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
