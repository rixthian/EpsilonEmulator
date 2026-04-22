using Epsilon.Games;
using Epsilon.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Epsilon.CoreGame.Tests;

public sealed class ExpandedCommandExecutionTests
{
    [Fact]
    public async Task LangCommand_ChangesInterfaceLanguage()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomEntryService roomEntryService = services.GetRequiredService<IRoomEntryService>();
        IRoomInteractionService roomInteractionService = services.GetRequiredService<IRoomInteractionService>();
        IInterfacePreferenceService interfacePreferenceService = services.GetRequiredService<IInterfacePreferenceService>();

        await roomEntryService.EnterAsync(new RoomEntryRequest(new CharacterId(7), new RoomId(1), null, false), cancellationToken);

        RoomChatResult result = await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(7), new RoomId(1), ":lang es"),
            cancellationToken);

        InterfacePreferenceSnapshot snapshot =
            await interfacePreferenceService.GetSnapshotAsync(new CharacterId(7), cancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal("es", snapshot.SelectedLanguageCode);
    }

    [Fact]
    public async Task WaveCommand_UpdatesActorStatus()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomEntryService roomEntryService = services.GetRequiredService<IRoomEntryService>();
        IRoomInteractionService roomInteractionService = services.GetRequiredService<IRoomInteractionService>();
        IRoomRuntimeRepository roomRuntimeRepository = services.GetRequiredService<IRoomRuntimeRepository>();

        await roomEntryService.EnterAsync(new RoomEntryRequest(new CharacterId(7), new RoomId(1), null, false), cancellationToken);

        RoomChatResult result = await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(7), new RoomId(1), ":wave"),
            cancellationToken);

        RoomActorState? actor =
            await roomRuntimeRepository.GetActorByIdAsync(new RoomId(1), 7, cancellationToken);

        Assert.True(result.Succeeded);
        Assert.NotNull(actor);
        Assert.Contains(actor!.StatusEntries, entry => entry.Key == "wav" && entry.Value == "1");
    }

    [Fact]
    public async Task BattleBallLifecycleCommands_WorkForAdministrativeRank()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomEntryService roomEntryService = services.GetRequiredService<IRoomEntryService>();
        IRoomInteractionService roomInteractionService = services.GetRequiredService<IRoomInteractionService>();
        Epsilon.Games.IGameRuntimeService gameRuntimeService = services.GetRequiredService<Epsilon.Games.IGameRuntimeService>();

        await roomEntryService.EnterAsync(new RoomEntryRequest(new CharacterId(5), new RoomId(1), null, false), cancellationToken);

        RoomChatResult prepareResult = await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(5), new RoomId(1), ":bbprepare battleball-public-1"),
            cancellationToken);
        RoomChatResult startResult = await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(5), new RoomId(1), ":bbstart battleball-public-1"),
            cancellationToken);
        RoomChatResult scoreResult = await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(5), new RoomId(1), ":bbscore battleball-public-1 red 3"),
            cancellationToken);

        Epsilon.Games.GameSessionState? session =
            await gameRuntimeService.GetSessionAsync("battleball-public-1", cancellationToken);

        Assert.True(prepareResult.Succeeded);
        Assert.True(startResult.Succeeded);
        Assert.True(scoreResult.Succeeded);
        Assert.NotNull(session);
        Assert.Equal(Epsilon.Games.GameSessionStatus.Running, session!.Status);
        Assert.Contains(session.Teams, team => team.TeamKey == "red" && team.ScoreValue >= 3);
    }

    private static ServiceProvider BuildServices()
    {
        ConfigurationManager configuration = new();
        configuration["Infrastructure:Provider"] = "InMemory";
        configuration["Infrastructure:RedisConnectionString"] = "localhost:6379";

        ServiceCollection services = new();
        services.AddPersistenceRuntime(configuration);
        services.AddCoreGameRuntime();
        services.AddGameRuntime();
        return services.BuildServiceProvider();
    }
}
