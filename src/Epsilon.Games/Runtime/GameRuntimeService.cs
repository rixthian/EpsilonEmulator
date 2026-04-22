namespace Epsilon.Games;

public sealed class GameRuntimeService : IGameRuntimeService
{
    private readonly IGameDefinitionRepository _gameDefinitions;
    private readonly IGameSessionRepository _gameSessions;

    public GameRuntimeService(
        IGameDefinitionRepository gameDefinitions,
        IGameSessionRepository gameSessions)
    {
        _gameDefinitions = gameDefinitions;
        _gameSessions = gameSessions;
    }

    public async ValueTask<IReadOnlyList<GameSessionState>> GetActiveSessionsAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<GameDefinition> games = await _gameDefinitions.GetAllAsync(cancellationToken);
        List<GameSessionState> sessions = [];

        foreach (GameDefinition game in games.Where(candidate => candidate.IsEnabled))
        {
            IReadOnlyList<GameSessionState> gameSessions = await _gameSessions.GetActiveByGameKeyAsync(
                game.GameKey,
                cancellationToken);
            sessions.AddRange(gameSessions);
        }

        return sessions
            .OrderBy(candidate => candidate.GameKey, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.SessionKey, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public ValueTask<GameSessionState?> GetSessionAsync(
        string sessionKey,
        CancellationToken cancellationToken = default)
    {
        return _gameSessions.GetBySessionKeyAsync(sessionKey, cancellationToken);
    }
}
