using Epsilon.Games;
using Epsilon.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Epsilon.CoreGame.Tests;

public sealed class GameRuntimeServiceTests
{
    [Fact]
    public async Task GetActiveSessions_ReturnsSeededGameSessions()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IGameRuntimeService gameRuntimeService = services.GetRequiredService<IGameRuntimeService>();

        IReadOnlyList<GameSessionState> sessions = await gameRuntimeService.GetActiveSessionsAsync(cancellationToken);

        Assert.Equal(3, sessions.Count);
        Assert.Contains(sessions, session => session.GameKey == "battleball" && session.Status == GameSessionStatus.Running);
        Assert.Contains(sessions, session => session.GameKey == "snowstorm" && session.Status == GameSessionStatus.Preparing);
        Assert.Contains(sessions, session => session.GameKey == "wobblesquabble" && session.IsPrivateMatch);
    }

    [Fact]
    public async Task GetSession_ReturnsExpectedTeamsAndPlayers()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IGameRuntimeService gameRuntimeService = services.GetRequiredService<IGameRuntimeService>();

        GameSessionState? session = await gameRuntimeService.GetSessionAsync("battleball-public-1", cancellationToken);

        Assert.NotNull(session);
        Assert.Equal(2, session!.Teams.Count);
        Assert.Equal(4, session.Players.Count);
        Assert.Contains(session.Teams, team => team.TeamKey == "red" && team.ScoreValue == 42);
        Assert.Contains(session.Players, player => player.DisplayName == "epsilon" && player.TeamKey == "red");
    }

    private static ServiceProvider BuildServices()
    {
        ConfigurationManager configuration = new();
        configuration["Infrastructure:Provider"] = "InMemory";

        ServiceCollection services = new();
        services.AddPersistenceRuntime(configuration);
        services.AddCoreGameRuntime();
        services.AddGameRuntime();
        return services.BuildServiceProvider();
    }
}
