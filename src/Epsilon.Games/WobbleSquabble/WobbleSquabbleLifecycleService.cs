namespace Epsilon.Games;

public sealed class WobbleSquabbleLifecycleService : IWobbleSquabbleLifecycleService
{
    private readonly IGameSessionRepository _gameSessionRepository;

    public WobbleSquabbleLifecycleService(IGameSessionRepository gameSessionRepository)
    {
        _gameSessionRepository = gameSessionRepository;
    }

    public async ValueTask<GameSessionUpdateResult> PrepareDuelAsync(
        string sessionKey,
        CancellationToken cancellationToken = default)
    {
        GameSessionState? session = await ResolveWobbleSessionAsync(sessionKey, cancellationToken);
        if (session is null)
        {
            return new GameSessionUpdateResult(false, "Wobble Squabble session could not be resolved.", null);
        }

        if (session.Status is not (GameSessionStatus.Waiting or GameSessionStatus.Preparing or GameSessionStatus.Finished))
        {
            return new GameSessionUpdateResult(false, "Wobble Squabble duel can only be prepared from waiting, preparing, or finished state.", session);
        }

        IReadOnlyList<GameTeamDefinition> preparedTeams = session.Teams
            .Select(team => team with { ScoreValue = 0 })
            .ToArray();

        GameSessionState updatedSession = session with
        {
            Status = GameSessionStatus.Preparing,
            PhaseCode = "duelist_lock",
            Teams = preparedTeams
        };

        await _gameSessionRepository.StoreAsync(updatedSession, cancellationToken);
        return new GameSessionUpdateResult(true, "Wobble Squabble duel is now preparing.", updatedSession);
    }

    public async ValueTask<GameSessionUpdateResult> StartDuelAsync(
        string sessionKey,
        CancellationToken cancellationToken = default)
    {
        GameSessionState? session = await ResolveWobbleSessionAsync(sessionKey, cancellationToken);
        if (session is null)
        {
            return new GameSessionUpdateResult(false, "Wobble Squabble session could not be resolved.", null);
        }

        if (session.Status is not (GameSessionStatus.Waiting or GameSessionStatus.Preparing))
        {
            return new GameSessionUpdateResult(false, "Wobble Squabble duel can only start from waiting or preparing state.", session);
        }

        if (session.Players.Count(player => player.IsConnected) < 2)
        {
            return new GameSessionUpdateResult(false, "Wobble Squabble requires at least 2 active duelists to start.", session);
        }

        GameSessionState updatedSession = session with
        {
            Status = GameSessionStatus.Running,
            PhaseCode = "duel_active"
        };

        await _gameSessionRepository.StoreAsync(updatedSession, cancellationToken);
        return new GameSessionUpdateResult(true, "Wobble Squabble duel started.", updatedSession);
    }

    public async ValueTask<GameSessionUpdateResult> AwardPointsAsync(
        string sessionKey,
        string teamKey,
        int points,
        CancellationToken cancellationToken = default)
    {
        GameSessionState? session = await ResolveWobbleSessionAsync(sessionKey, cancellationToken);
        if (session is null)
        {
            return new GameSessionUpdateResult(false, "Wobble Squabble session could not be resolved.", null);
        }

        if (session.Status != GameSessionStatus.Running)
        {
            return new GameSessionUpdateResult(false, "Wobble Squabble score updates require an active duel.", session);
        }

        if (string.IsNullOrWhiteSpace(teamKey) || points <= 0)
        {
            return new GameSessionUpdateResult(false, "Team key and positive points are required.", session);
        }

        if (!session.Teams.Any(candidate => string.Equals(candidate.TeamKey, teamKey, StringComparison.OrdinalIgnoreCase)))
        {
            return new GameSessionUpdateResult(false, $"Wobble Squabble team '{teamKey}' could not be resolved.", session);
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
        return new GameSessionUpdateResult(true, $"Wobble Squabble team '{teamKey}' received {points} point(s).", updatedSession);
    }

    public async ValueTask<GameSessionUpdateResult> FinishDuelAsync(
        string sessionKey,
        CancellationToken cancellationToken = default)
    {
        GameSessionState? session = await ResolveWobbleSessionAsync(sessionKey, cancellationToken);
        if (session is null)
        {
            return new GameSessionUpdateResult(false, "Wobble Squabble session could not be resolved.", null);
        }

        if (session.Status == GameSessionStatus.Finished)
        {
            return new GameSessionUpdateResult(true, "Wobble Squabble duel is already finished.", session);
        }

        GameTeamDefinition? winner = session.Teams
            .OrderByDescending(team => team.ScoreValue)
            .FirstOrDefault();

        string winnerDetail = winner is not null
            ? $" Winner: {winner.DisplayName} ({winner.ScoreValue} point(s))."
            : string.Empty;

        GameSessionState updatedSession = session with
        {
            Status = GameSessionStatus.Finished,
            PhaseCode = "duel_complete"
        };

        await _gameSessionRepository.StoreAsync(updatedSession, cancellationToken);
        return new GameSessionUpdateResult(true, $"Wobble Squabble duel finished.{winnerDetail}", updatedSession);
    }

    private async ValueTask<GameSessionState?> ResolveWobbleSessionAsync(
        string sessionKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sessionKey))
        {
            return null;
        }

        GameSessionState? session = await _gameSessionRepository.GetBySessionKeyAsync(sessionKey, cancellationToken);
        if (session is null || !string.Equals(session.GameKey, "wobblesquabble", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return session;
    }
}
