using Epsilon.Games;
using Epsilon.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Epsilon.CoreGame.Tests;

public sealed class BattleBallLifecycleTests
{
    [Fact]
    public async Task PrepareAndStartRound_UpdateBattleBallSessionState()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using ServiceProvider services = BuildServices();
        IBattleBallLifecycleService lifecycleService = services.GetRequiredService<IBattleBallLifecycleService>();
        IGameRuntimeService runtimeService = services.GetRequiredService<IGameRuntimeService>();
        IGameSessionRepository gameSessionRepository = services.GetRequiredService<IGameSessionRepository>();

        GameSessionState seededSession =
            (await runtimeService.GetSessionAsync("battleball-public-1", cancellationToken))!;
        await gameSessionRepository.StoreAsync(
            seededSession with
            {
                Status = GameSessionStatus.Waiting,
                PhaseCode = "matchmaking"
            },
            cancellationToken);

        GameSessionUpdateResult prepareResult = await lifecycleService.PrepareMatchAsync("battleball-public-1", cancellationToken);
        GameSessionUpdateResult startResult = await lifecycleService.StartRoundAsync("battleball-public-1", cancellationToken);
        GameSessionState? session = await runtimeService.GetSessionAsync("battleball-public-1", cancellationToken);

        Assert.True(prepareResult.Succeeded);
        Assert.True(startResult.Succeeded);
        Assert.NotNull(session);
        Assert.Equal(GameSessionStatus.Running, session!.Status);
        Assert.Equal("round_live", session.PhaseCode);
    }

    [Fact]
    public async Task AwardPoints_IncrementsRequestedTeamScore()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using ServiceProvider services = BuildServices();
        IBattleBallLifecycleService lifecycleService = services.GetRequiredService<IBattleBallLifecycleService>();
        IGameRuntimeService runtimeService = services.GetRequiredService<IGameRuntimeService>();

        GameSessionUpdateResult result = await lifecycleService.AwardPointsAsync("battleball-public-1", "red", 5, cancellationToken);
        GameSessionState? session = await runtimeService.GetSessionAsync("battleball-public-1", cancellationToken);

        Assert.True(result.Succeeded);
        Assert.NotNull(session);
        Assert.Contains(session!.Teams, team => team.TeamKey == "red" && team.ScoreValue == 47);
    }

    [Fact]
    public async Task FinishMatch_MarksBattleBallSessionAsFinished()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using ServiceProvider services = BuildServices();
        IBattleBallLifecycleService lifecycleService = services.GetRequiredService<IBattleBallLifecycleService>();
        IGameRuntimeService runtimeService = services.GetRequiredService<IGameRuntimeService>();

        GameSessionUpdateResult result = await lifecycleService.FinishMatchAsync("battleball-public-1", cancellationToken);
        GameSessionState? session = await runtimeService.GetSessionAsync("battleball-public-1", cancellationToken);

        Assert.True(result.Succeeded);
        Assert.NotNull(session);
        Assert.Equal(GameSessionStatus.Finished, session!.Status);
        Assert.Equal("match_complete", session.PhaseCode);
    }

    [Fact]
    public async Task PrepareMatch_AfterFinish_ResetsScoresAndReopensSession()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using ServiceProvider services = BuildServices();
        IBattleBallLifecycleService lifecycleService = services.GetRequiredService<IBattleBallLifecycleService>();
        IGameRuntimeService runtimeService = services.GetRequiredService<IGameRuntimeService>();

        await lifecycleService.FinishMatchAsync("battleball-public-1", cancellationToken);
        GameSessionUpdateResult prepareResult = await lifecycleService.PrepareMatchAsync("battleball-public-1", cancellationToken);
        GameSessionState? session = await runtimeService.GetSessionAsync("battleball-public-1", cancellationToken);

        Assert.True(prepareResult.Succeeded);
        Assert.NotNull(session);
        Assert.Equal(GameSessionStatus.Preparing, session!.Status);
        Assert.Equal("team_lock", session.PhaseCode);
        Assert.All(session.Teams, team => Assert.Equal(0, team.ScoreValue));
    }

    [Fact]
    public async Task PrepareMatch_RejectsNonBattleBallSession()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using ServiceProvider services = BuildServices();
        IBattleBallLifecycleService lifecycleService = services.GetRequiredService<IBattleBallLifecycleService>();

        GameSessionUpdateResult result = await lifecycleService.PrepareMatchAsync("snowstorm-public-1", cancellationToken);

        Assert.False(result.Succeeded);
        Assert.Null(result.Session);
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
