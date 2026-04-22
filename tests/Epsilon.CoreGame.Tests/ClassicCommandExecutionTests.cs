using Epsilon.Games;
using Epsilon.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Epsilon.CoreGame.Tests;

public sealed class ClassicCommandExecutionTests
{
    [Fact]
    public async Task Chooser_ReturnsVisiblePlayersInRoom()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomInteractionService roomInteractionService = services.GetRequiredService<IRoomInteractionService>();

        RoomChatResult result = await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(1), new RoomId(1), ":chooser"),
            cancellationToken);

        Assert.True(result.Succeeded);
        Assert.Contains("epsilon", result.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Furni_ReturnsVisibleRoomItemLabels()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomInteractionService roomInteractionService = services.GetRequiredService<IRoomInteractionService>();

        RoomChatResult result = await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(1), new RoomId(1), ":furni"),
            cancellationToken);

        Assert.True(result.Succeeded);
        Assert.Contains("sofa", result.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Whisper_AppendsWhisperMessageKind()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomEntryService roomEntryService = services.GetRequiredService<IRoomEntryService>();
        IRoomInteractionService roomInteractionService = services.GetRequiredService<IRoomInteractionService>();
        IRoomRuntimeRepository roomRuntimeRepository = services.GetRequiredService<IRoomRuntimeRepository>();

        await roomEntryService.EnterAsync(new RoomEntryRequest(new CharacterId(7), new RoomId(1), null, false), cancellationToken);

        RoomChatResult result = await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(1), new RoomId(1), ":whisper vector hello there"),
            cancellationToken);

        IReadOnlyList<RoomChatMessage> messages =
            await roomRuntimeRepository.GetChatMessagesByRoomIdAsync(new RoomId(1), cancellationToken);

        Assert.True(result.Succeeded);
        Assert.Contains(messages, message => message.MessageKind == RoomChatMessageKind.Whisper);
    }

    [Fact]
    public async Task Shout_AppendsShoutMessageKind()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomEntryService roomEntryService = services.GetRequiredService<IRoomEntryService>();
        IRoomInteractionService roomInteractionService = services.GetRequiredService<IRoomInteractionService>();
        IRoomRuntimeRepository roomRuntimeRepository = services.GetRequiredService<IRoomRuntimeRepository>();

        await roomEntryService.EnterAsync(new RoomEntryRequest(new CharacterId(7), new RoomId(1), null, false), cancellationToken);

        RoomChatResult result = await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(1), new RoomId(1), ":shout vector we need backup"),
            cancellationToken);

        IReadOnlyList<RoomChatMessage> messages =
            await roomRuntimeRepository.GetChatMessagesByRoomIdAsync(new RoomId(1), cancellationToken);

        Assert.True(result.Succeeded);
        Assert.Contains(messages, message => message.MessageKind == RoomChatMessageKind.Shout);
    }

    private static ServiceProvider BuildServices()
    {
        ConfigurationManager configuration = new();
        configuration["Infrastructure:Provider"] = "InMemory";
        configuration["Infrastructure:RedisConnectionString"] = "localhost:6379";

        ServiceCollection services = new();
        services.AddPersistenceRuntime(configuration);
        services.AddGameRuntime();
        services.AddCoreGameRuntime();
        return services.BuildServiceProvider();
    }
}
