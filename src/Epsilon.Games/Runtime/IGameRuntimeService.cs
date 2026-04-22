namespace Epsilon.Games;

public interface IGameRuntimeService
{
    ValueTask<IReadOnlyList<GameSessionState>> GetActiveSessionsAsync(
        CancellationToken cancellationToken = default);

    ValueTask<GameSessionState?> GetSessionAsync(
        string sessionKey,
        CancellationToken cancellationToken = default);
}
