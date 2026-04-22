namespace Epsilon.Games;

public sealed class BattleBallLifecycleService : IBattleBallLifecycleService
{
    private readonly IGameSessionRepository _gameSessionRepository;

    public BattleBallLifecycleService(IGameSessionRepository gameSessionRepository)
    {
        _gameSessionRepository = gameSessionRepository;
    }

    public async ValueTask<GameSessionUpdateResult> PrepareMatchAsync(
        string sessionKey,
        CancellationToken cancellationToken = default)
    {
        GameSessionState? session = await ResolveBattleBallSessionAsync(sessionKey, cancellationToken);
        if (session is null)
        {
            return new GameSessionUpdateResult(false, "BattleBall session could not be resolved.", null);
        }

        if (session.Status is not (GameSessionStatus.Waiting or GameSessionStatus.Preparing or GameSessionStatus.Finished))
        {
            return new GameSessionUpdateResult(false, "BattleBall match can only be prepared from waiting, preparing, or finished state.", session);
        }

        IReadOnlyList<GameTeamDefinition> preparedTeams = session.Teams
            .Select(team => team with { ScoreValue = 0 })
            .ToArray();

        GameSessionState updatedSession = session with
        {
            Status = GameSessionStatus.Preparing,
            PhaseCode = "team_lock",
            Teams = preparedTeams
        };

        await _gameSessionRepository.StoreAsync(updatedSession, cancellationToken);
        return new GameSessionUpdateResult(true, "BattleBall match is now preparing.", updatedSession);
    }

    public async ValueTask<GameSessionUpdateResult> StartRoundAsync(
        string sessionKey,
        CancellationToken cancellationToken = default)
    {
        GameSessionState? session = await ResolveBattleBallSessionAsync(sessionKey, cancellationToken);
        if (session is null)
        {
            return new GameSessionUpdateResult(false, "BattleBall session could not be resolved.", null);
        }

        if (session.Status is not (GameSessionStatus.Waiting or GameSessionStatus.Preparing))
        {
            return new GameSessionUpdateResult(false, "BattleBall round can only start from waiting or preparing state.", session);
        }

        GameSessionState updatedSession = session with
        {
            Status = GameSessionStatus.Running,
            PhaseCode = "round_live"
        };

        await _gameSessionRepository.StoreAsync(updatedSession, cancellationToken);
        return new GameSessionUpdateResult(true, "BattleBall round started.", updatedSession);
    }

    public async ValueTask<GameSessionUpdateResult> AwardPointsAsync(
        string sessionKey,
        string teamKey,
        int points,
        CancellationToken cancellationToken = default)
    {
        GameSessionState? session = await ResolveBattleBallSessionAsync(sessionKey, cancellationToken);
        if (session is null)
        {
            return new GameSessionUpdateResult(false, "BattleBall session could not be resolved.", null);
        }

        if (session.Status != GameSessionStatus.Running)
        {
            return new GameSessionUpdateResult(false, "BattleBall score updates require a running match.", session);
        }

        if (string.IsNullOrWhiteSpace(teamKey) || points <= 0)
        {
            return new GameSessionUpdateResult(false, "Team key and positive points are required.", session);
        }

        if (!session.Teams.Any(candidate => string.Equals(candidate.TeamKey, teamKey, StringComparison.OrdinalIgnoreCase)))
        {
            return new GameSessionUpdateResult(false, $"BattleBall team '{teamKey}' could not be resolved.", session);
        }

        IReadOnlyList<GameTeamDefinition> updatedTeams = session.Teams
            .Select(team => string.Equals(team.TeamKey, teamKey, StringComparison.OrdinalIgnoreCase)
                ? team with { ScoreValue = team.ScoreValue + points }
                : team)
            .ToArray();

        GameSessionState updatedSession = session with
        {
            Teams = updatedTeams
        };

        await _gameSessionRepository.StoreAsync(updatedSession, cancellationToken);
        return new GameSessionUpdateResult(true, $"BattleBall team '{teamKey}' received {points} point(s).", updatedSession);
    }

    public async ValueTask<GameSessionUpdateResult> FinishMatchAsync(
        string sessionKey,
        CancellationToken cancellationToken = default)
    {
        GameSessionState? session = await ResolveBattleBallSessionAsync(sessionKey, cancellationToken);
        if (session is null)
        {
            return new GameSessionUpdateResult(false, "BattleBall session could not be resolved.", null);
        }

        if (session.Status == GameSessionStatus.Finished)
        {
            return new GameSessionUpdateResult(true, "BattleBall match is already finished.", session);
        }

        GameSessionState updatedSession = session with
        {
            Status = GameSessionStatus.Finished,
            PhaseCode = "match_complete"
        };

        await _gameSessionRepository.StoreAsync(updatedSession, cancellationToken);
        return new GameSessionUpdateResult(true, "BattleBall match finished.", updatedSession);
    }

    private async ValueTask<GameSessionState?> ResolveBattleBallSessionAsync(
        string sessionKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sessionKey))
        {
            return null;
        }

        GameSessionState? session = await _gameSessionRepository.GetBySessionKeyAsync(sessionKey, cancellationToken);
        if (session is null || !string.Equals(session.GameKey, "battleball", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return session;
    }
}
