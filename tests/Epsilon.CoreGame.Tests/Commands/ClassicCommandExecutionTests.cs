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
    public async Task Whisper_DoesNotLeakIntoPublicRoomChatLog()
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
        Assert.DoesNotContain(messages, message => message.MessageKind == RoomChatMessageKind.Whisper);
    }

    [Fact]
    public async Task Whisper_IsStoredInPrivateChatPathForSenderAndRecipient()
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

        IReadOnlyList<RoomChatMessage> senderMessages =
            await roomRuntimeRepository.GetPrivateChatMessagesByActorAsync(new RoomId(1), 1, cancellationToken);
        IReadOnlyList<RoomChatMessage> recipientMessages =
            await roomRuntimeRepository.GetPrivateChatMessagesByActorAsync(new RoomId(1), 7, cancellationToken);

        Assert.True(result.Succeeded);
        Assert.Contains(senderMessages, message =>
            message.MessageKind == RoomChatMessageKind.Whisper &&
            message.RecipientActorId == 7 &&
            message.RecipientName == "vector" &&
            message.Message == "hello there");
        Assert.Contains(recipientMessages, message =>
            message.MessageKind == RoomChatMessageKind.Whisper &&
            message.SenderActorId == 1 &&
            message.Message == "hello there");
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

    [Fact]
    public async Task Link_AppendsNormalizedLinkMessageKind()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomInteractionService roomInteractionService = services.GetRequiredService<IRoomInteractionService>();
        IRoomRuntimeRepository roomRuntimeRepository = services.GetRequiredService<IRoomRuntimeRepository>();

        RoomChatResult result = await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(1), new RoomId(1), ":link habbo.com/community"),
            cancellationToken);

        IReadOnlyList<RoomChatMessage> messages =
            await roomRuntimeRepository.GetChatMessagesByRoomIdAsync(new RoomId(1), cancellationToken);

        Assert.True(result.Succeeded);
        Assert.Contains(messages, message =>
            message.MessageKind == RoomChatMessageKind.Link &&
            message.Message == "https://habbo.com/community");
    }

    [Fact]
    public async Task Link_RejectsNonHttpSchemes()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomInteractionService roomInteractionService = services.GetRequiredService<IRoomInteractionService>();

        RoomChatResult result = await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(1), new RoomId(1), ":link javascript:alert(1)"),
            cancellationToken);

        Assert.True(result.Succeeded);
        Assert.Contains("Only valid http or https links are allowed", result.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LidoBot_CanGrantDrinkCarryItemFromChatTrigger()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomEntryService roomEntryService = services.GetRequiredService<IRoomEntryService>();
        IRoomInteractionService roomInteractionService = services.GetRequiredService<IRoomInteractionService>();
        IRoomRuntimeRepository roomRuntimeRepository = services.GetRequiredService<IRoomRuntimeRepository>();

        await roomEntryService.EnterAsync(new RoomEntryRequest(new CharacterId(7), new RoomId(10), null, false), cancellationToken);

        RoomChatResult result = await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(7), new RoomId(10), "ice cream please"),
            cancellationToken);

        RoomActorState? actor = await roomRuntimeRepository.GetActorByIdAsync(new RoomId(10), 7, cancellationToken);
        IReadOnlyList<RoomChatMessage> messages =
            await roomRuntimeRepository.GetChatMessagesByRoomIdAsync(new RoomId(10), cancellationToken);

        Assert.True(result.Succeeded);
        Assert.NotNull(actor);
        Assert.NotNull(actor!.CarryItem);
        Assert.Equal("Ice Cream", actor.CarryItem!.DisplayName);
        Assert.Contains(messages, message => message.SenderName == "Lido Bar" && message.Message.Contains("ice cream", StringComparison.OrdinalIgnoreCase));
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
