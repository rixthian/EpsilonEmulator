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

    [Fact]
    public async Task Idle_TogglesIdleStatusEntry()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomInteractionService roomInteractionService = services.GetRequiredService<IRoomInteractionService>();
        IRoomRuntimeRepository roomRuntimeRepository = services.GetRequiredService<IRoomRuntimeRepository>();

        RoomChatResult result = await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(1), new RoomId(1), ":idle"),
            cancellationToken);

        RoomActorState? actor = await roomRuntimeRepository.GetActorByIdAsync(new RoomId(1), 1, cancellationToken);

        Assert.True(result.Succeeded);
        Assert.NotNull(actor);
        Assert.Contains(actor!.StatusEntries, entry => entry.Key == "idle");
    }

    [Fact]
    public async Task Kiss_SetsGestureStatus()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomInteractionService roomInteractionService = services.GetRequiredService<IRoomInteractionService>();
        IRoomRuntimeRepository roomRuntimeRepository = services.GetRequiredService<IRoomRuntimeRepository>();

        RoomChatResult result = await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(1), new RoomId(1), ":kiss"),
            cancellationToken);

        RoomActorState? actor = await roomRuntimeRepository.GetActorByIdAsync(new RoomId(1), 1, cancellationToken);

        Assert.True(result.Succeeded);
        Assert.NotNull(actor);
        Assert.Contains(actor!.StatusEntries, entry => entry.Key == "gest" && entry.Value == "kiss");
    }

    [Fact]
    public async Task Dance_SetsDanceStatus()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomInteractionService roomInteractionService = services.GetRequiredService<IRoomInteractionService>();
        IRoomRuntimeRepository roomRuntimeRepository = services.GetRequiredService<IRoomRuntimeRepository>();

        RoomChatResult result = await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(1), new RoomId(1), ":dance 3"),
            cancellationToken);

        RoomActorState? actor = await roomRuntimeRepository.GetActorByIdAsync(new RoomId(1), 1, cancellationToken);

        Assert.True(result.Succeeded);
        Assert.NotNull(actor);
        Assert.Contains(actor!.StatusEntries, entry => entry.Key == "dance" && entry.Value == "3");
    }

    [Fact]
    public async Task ShortcutWave_TriggersWaveStatus()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomInteractionService roomInteractionService = services.GetRequiredService<IRoomInteractionService>();
        IRoomRuntimeRepository roomRuntimeRepository = services.GetRequiredService<IRoomRuntimeRepository>();

        RoomChatResult result = await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(1), new RoomId(1), "o/"),
            cancellationToken);

        RoomActorState? actor = await roomRuntimeRepository.GetActorByIdAsync(new RoomId(1), 1, cancellationToken);

        Assert.True(result.Succeeded);
        Assert.NotNull(actor);
        Assert.Contains(actor!.StatusEntries, entry => entry.Key == "wav" && entry.Value == "1");
    }

    [Fact]
    public async Task ShortcutSmile_TriggersSmileEmote()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomInteractionService roomInteractionService = services.GetRequiredService<IRoomInteractionService>();
        IRoomRuntimeRepository roomRuntimeRepository = services.GetRequiredService<IRoomRuntimeRepository>();

        RoomChatResult result = await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(1), new RoomId(1), ":D"),
            cancellationToken);

        RoomActorState? actor = await roomRuntimeRepository.GetActorByIdAsync(new RoomId(1), 1, cancellationToken);

        Assert.True(result.Succeeded);
        Assert.NotNull(actor);
        Assert.Contains(actor!.StatusEntries, entry => entry.Key == "gest" && entry.Value == "smile");
    }

    [Fact]
    public async Task ShortcutBlowKiss_TriggersKissEmote()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomInteractionService roomInteractionService = services.GetRequiredService<IRoomInteractionService>();
        IRoomRuntimeRepository roomRuntimeRepository = services.GetRequiredService<IRoomRuntimeRepository>();

        RoomChatResult result = await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(1), new RoomId(1), "_b"),
            cancellationToken);

        RoomActorState? actor = await roomRuntimeRepository.GetActorByIdAsync(new RoomId(1), 1, cancellationToken);

        Assert.True(result.Succeeded);
        Assert.NotNull(actor);
        Assert.Contains(actor!.StatusEntries, entry => entry.Key == "gest" && entry.Value == "kiss");
    }

    [Fact]
    public async Task ShortcutColonX_TriggersKissEmote()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomInteractionService roomInteractionService = services.GetRequiredService<IRoomInteractionService>();
        IRoomRuntimeRepository roomRuntimeRepository = services.GetRequiredService<IRoomRuntimeRepository>();

        RoomChatResult result = await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(1), new RoomId(1), ":x"),
            cancellationToken);

        RoomActorState? actor = await roomRuntimeRepository.GetActorByIdAsync(new RoomId(1), 1, cancellationToken);

        Assert.True(result.Succeeded);
        Assert.NotNull(actor);
        Assert.Contains(actor!.StatusEntries, entry => entry.Key == "gest" && entry.Value == "kiss");
    }

    [Fact]
    public async Task Drop_ClearsCarryItem()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomInteractionService roomInteractionService = services.GetRequiredService<IRoomInteractionService>();
        IRoomRuntimeRepository roomRuntimeRepository = services.GetRequiredService<IRoomRuntimeRepository>();

        RoomChatResult carryResult = await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(1), new RoomId(1), ":carry 7"),
            cancellationToken);

        RoomChatResult dropResult = await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(1), new RoomId(1), ":drop"),
            cancellationToken);

        RoomActorState? actor = await roomRuntimeRepository.GetActorByIdAsync(new RoomId(1), 1, cancellationToken);

        Assert.True(carryResult.Succeeded);
        Assert.True(dropResult.Succeeded);
        Assert.NotNull(actor);
        Assert.Null(actor!.CarryItem);
    }

    [Fact]
    public async Task MuteBots_SuppressesScriptedBotReplies()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomEntryService roomEntryService = services.GetRequiredService<IRoomEntryService>();
        IRoomInteractionService roomInteractionService = services.GetRequiredService<IRoomInteractionService>();
        IRoomRuntimeRepository roomRuntimeRepository = services.GetRequiredService<IRoomRuntimeRepository>();

        await roomEntryService.EnterAsync(new RoomEntryRequest(new CharacterId(7), new RoomId(10), null, false), cancellationToken);

        RoomChatResult muteResult = await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(7), new RoomId(10), ":mutebots"),
            cancellationToken);

        RoomChatResult chatResult = await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(7), new RoomId(10), "ice cream please"),
            cancellationToken);

        RoomActorState? actor = await roomRuntimeRepository.GetActorByIdAsync(new RoomId(10), 7, cancellationToken);
        IReadOnlyList<RoomChatMessage> messages =
            await roomRuntimeRepository.GetChatMessagesByRoomIdAsync(new RoomId(10), cancellationToken);

        Assert.True(muteResult.Succeeded);
        Assert.True(chatResult.Succeeded);
        Assert.NotNull(actor);
        Assert.Null(actor!.CarryItem);
        Assert.DoesNotContain(messages, message => message.SenderName == "Lido Bar");
    }

    [Fact]
    public async Task Respect_TransfersDailyRespectToRoomPresentUser()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomEntryService roomEntryService = services.GetRequiredService<IRoomEntryService>();
        IRoomInteractionService roomInteractionService = services.GetRequiredService<IRoomInteractionService>();
        ICharacterProfileRepository characterProfileRepository = services.GetRequiredService<ICharacterProfileRepository>();

        await roomEntryService.EnterAsync(new RoomEntryRequest(new CharacterId(7), new RoomId(1), null, false), cancellationToken);

        CharacterProfile? senderBefore = await characterProfileRepository.GetByIdAsync(new CharacterId(1), cancellationToken);
        CharacterProfile? targetBefore = await characterProfileRepository.GetByIdAsync(new CharacterId(7), cancellationToken);

        RoomChatResult result = await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(1), new RoomId(1), ":respect vector"),
            cancellationToken);

        CharacterProfile? senderAfter = await characterProfileRepository.GetByIdAsync(new CharacterId(1), cancellationToken);
        CharacterProfile? targetAfter = await characterProfileRepository.GetByIdAsync(new CharacterId(7), cancellationToken);

        Assert.NotNull(senderBefore);
        Assert.NotNull(targetBefore);
        Assert.True(result.Succeeded);
        Assert.Contains("Respect sent to vector", result.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(senderAfter);
        Assert.NotNull(targetAfter);
        Assert.Equal(senderBefore!.DailyRespectPoints - 1, senderAfter!.DailyRespectPoints);
        Assert.Equal(targetBefore!.RespectPoints + 1, targetAfter!.RespectPoints);
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
