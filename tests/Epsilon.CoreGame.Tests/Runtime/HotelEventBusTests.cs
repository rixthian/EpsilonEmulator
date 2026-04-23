using Epsilon.Games;
using Epsilon.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Epsilon.CoreGame.Tests;

public sealed class HotelEventBusTests
{
    [Fact]
    public async Task EnterAsync_PublishesRoomEntryCompletedEvent()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomEntryService roomEntryService = services.GetRequiredService<IRoomEntryService>();
        IHotelEventBus hotelEventBus = services.GetRequiredService<IHotelEventBus>();

        await roomEntryService.EnterAsync(
            new RoomEntryRequest(new CharacterId(7), new RoomId(1), null, false),
            cancellationToken);

        IReadOnlyList<HotelEventEnvelope> events = await hotelEventBus.GetRecentAsync(16, cancellationToken);
        RoomEntryCompletedEvent entryEvent = Assert.IsType<RoomEntryCompletedEvent>(
            Assert.Single(events, item => item.Kind == HotelEventKind.RoomEntryCompleted).Payload);

        Assert.Equal(7, entryEvent.CharacterId.Value);
        Assert.Equal(1, entryEvent.RoomId.Value);
    }

    [Fact]
    public async Task MoveActorAsync_PublishesRoomActorMovedEvent()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomInteractionService roomInteractionService = services.GetRequiredService<IRoomInteractionService>();
        IHotelEventBus hotelEventBus = services.GetRequiredService<IHotelEventBus>();

        await roomInteractionService.MoveActorAsync(
            new RoomActorMovementRequest(new CharacterId(1), new RoomId(1), 6, 5),
            cancellationToken);

        IReadOnlyList<HotelEventEnvelope> events = await hotelEventBus.GetRecentAsync(16, cancellationToken);
        RoomActorMovedEvent movedEvent = Assert.IsType<RoomActorMovedEvent>(
            Assert.Single(events, item => item.Kind == HotelEventKind.RoomActorMoved).Payload);

        Assert.Equal(1, movedEvent.CharacterId.Value);
        Assert.Equal(1, movedEvent.RoomId.Value);
        Assert.Equal(6, movedEvent.ToPosition.X);
        Assert.Equal(5, movedEvent.ToPosition.Y);
    }

    [Fact]
    public async Task SendChatAsync_PublishesChatMessageEvent()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomInteractionService roomInteractionService = services.GetRequiredService<IRoomInteractionService>();
        IHotelEventBus hotelEventBus = services.GetRequiredService<IHotelEventBus>();

        await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(1), new RoomId(1), "event bus check"),
            cancellationToken);

        IReadOnlyList<HotelEventEnvelope> events = await hotelEventBus.GetRecentAsync(16, cancellationToken);
        ChatMessagePublishedEvent messageEvent = Assert.IsType<ChatMessagePublishedEvent>(
            Assert.Single(events, item => item.Kind == HotelEventKind.ChatMessagePublished).Payload);

        Assert.Equal(RoomChatMessageKind.User, messageEvent.MessageKind);
        Assert.Equal("event bus check", messageEvent.Message);
    }

    [Fact]
    public async Task BanCommand_PublishesModerationActionEvent()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomEntryService roomEntryService = services.GetRequiredService<IRoomEntryService>();
        IRoomInteractionService roomInteractionService = services.GetRequiredService<IRoomInteractionService>();
        IHotelEventBus hotelEventBus = services.GetRequiredService<IHotelEventBus>();

        await roomEntryService.EnterAsync(
            new RoomEntryRequest(new CharacterId(7), new RoomId(1), null, false),
            cancellationToken);
        await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(1), new RoomId(1), ":ban vector 30 testing"),
            cancellationToken);

        IReadOnlyList<HotelEventEnvelope> events = await hotelEventBus.GetRecentAsync(16, cancellationToken);
        ModerationActionExecutedEvent moderationEvent = Assert.IsType<ModerationActionExecutedEvent>(
            Assert.Single(events, item => item.Kind == HotelEventKind.ModerationActionExecuted).Payload);

        Assert.Equal("ban", moderationEvent.ActionKey);
        Assert.Equal("vector", moderationEvent.TargetName);
    }

    [Fact]
    public async Task BotCreateCommand_PublishesBotConfigurationChangedEvent()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomInteractionService roomInteractionService = services.GetRequiredService<IRoomInteractionService>();
        IHotelEventBus hotelEventBus = services.GetRequiredService<IHotelEventBus>();

        await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(1), new RoomId(1), ":botcreate Helper"),
            cancellationToken);

        IReadOnlyList<HotelEventEnvelope> events = await hotelEventBus.GetRecentAsync(16, cancellationToken);
        BotConfigurationChangedEvent botEvent = Assert.IsType<BotConfigurationChangedEvent>(
            Assert.Single(events, item => item.Kind == HotelEventKind.BotConfigurationChanged).Payload);

        Assert.Equal("Helper", botEvent.BotName);
        Assert.Equal("create", botEvent.ChangeKind);
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
