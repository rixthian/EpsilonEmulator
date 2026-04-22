using Epsilon.Games;

namespace Epsilon.Persistence;

internal sealed class InMemoryGameSessionRepository : IGameSessionRepository
{
    private readonly InMemoryHotelStore _store;
    private readonly Lock _gate = new();

    public InMemoryGameSessionRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<IReadOnlyList<GameSessionState>> GetActiveByGameKeyAsync(
        string gameKey,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<GameSessionState> sessions = _store.GameSessions
            .Where(candidate =>
                string.Equals(candidate.GameKey, gameKey, StringComparison.OrdinalIgnoreCase) &&
                candidate.Status is GameSessionStatus.Waiting or GameSessionStatus.Preparing or GameSessionStatus.Running)
            .OrderBy(candidate => candidate.StartedAtUtc)
            .ToArray();

        return ValueTask.FromResult(sessions);
    }

    public ValueTask<GameSessionState?> GetBySessionKeyAsync(
        string sessionKey,
        CancellationToken cancellationToken = default)
    {
        GameSessionState? session = _store.GameSessions.FirstOrDefault(candidate =>
            string.Equals(candidate.SessionKey, sessionKey, StringComparison.OrdinalIgnoreCase));

        return ValueTask.FromResult(session);
    }

    public ValueTask StoreAsync(
        GameSessionState session,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            int index = _store.GameSessions.FindIndex(candidate =>
                string.Equals(candidate.SessionKey, session.SessionKey, StringComparison.OrdinalIgnoreCase));

            if (index >= 0)
            {
                _store.GameSessions[index] = session;
            }
            else
            {
                _store.GameSessions.Add(session);
            }
        }

        return ValueTask.CompletedTask;
    }
}
