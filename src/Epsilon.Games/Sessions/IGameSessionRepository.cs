namespace Epsilon.Games;

public interface IGameSessionRepository
{
    ValueTask<IReadOnlyList<GameSessionState>> GetActiveByGameKeyAsync(
        string gameKey,
        CancellationToken cancellationToken = default);

    ValueTask<GameSessionState?> GetBySessionKeyAsync(
        string sessionKey,
        CancellationToken cancellationToken = default);

    ValueTask StoreAsync(
        GameSessionState session,
        CancellationToken cancellationToken = default);
}
