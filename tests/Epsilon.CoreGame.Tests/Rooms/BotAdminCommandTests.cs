using Epsilon.Games;
using Epsilon.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Epsilon.CoreGame.Tests;

public sealed class BotAdminCommandTests
{
    [Fact]
    public async Task BotCreate_RegistersBotInCurrentRoom()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomInteractionService roomInteractionService = services.GetRequiredService<IRoomInteractionService>();
        IRoomRuntimeRepository roomRuntimeRepository = services.GetRequiredService<IRoomRuntimeRepository>();

        RoomChatResult result = await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(1), new RoomId(1), ":botcreate Helper"),
            cancellationToken);

        IReadOnlyList<RoomActorState> actors =
            await roomRuntimeRepository.GetActorsByRoomIdAsync(new RoomId(1), cancellationToken);

        Assert.True(result.Succeeded);
        Assert.Contains(actors, actor => actor.ActorKind == RoomActorKind.Bot && actor.DisplayName == "Helper");
    }

    [Fact]
    public async Task BotMove_UpdatesBotRuntimePosition()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomInteractionService roomInteractionService = services.GetRequiredService<IRoomInteractionService>();
        IRoomRuntimeRepository roomRuntimeRepository = services.GetRequiredService<IRoomRuntimeRepository>();

        await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(1), new RoomId(1), ":botcreate Helper"),
            cancellationToken);

        RoomChatResult result = await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(1), new RoomId(1), ":botmove Helper 10 5"),
            cancellationToken);

        RoomActorState helper = Assert.Single(
            await roomRuntimeRepository.GetActorsByRoomIdAsync(new RoomId(1), cancellationToken),
            actor => actor.ActorKind == RoomActorKind.Bot && actor.DisplayName == "Helper");

        Assert.True(result.Succeeded);
        Assert.Equal(10, helper.Position.X);
        Assert.Equal(5, helper.Position.Y);
    }

    [Fact]
    public async Task BotPatrol_SetsFirstWaypointAsGoal()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomInteractionService roomInteractionService = services.GetRequiredService<IRoomInteractionService>();
        IRoomRuntimeRepository roomRuntimeRepository = services.GetRequiredService<IRoomRuntimeRepository>();

        await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(1), new RoomId(1), ":botcreate Helper"),
            cancellationToken);

        RoomChatResult result = await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(1), new RoomId(1), ":botpatrol Helper 9 5 3 10 5 3"),
            cancellationToken);

        RoomActorState helper = Assert.Single(
            await roomRuntimeRepository.GetActorsByRoomIdAsync(new RoomId(1), cancellationToken),
            actor => actor.ActorKind == RoomActorKind.Bot && actor.DisplayName == "Helper");

        Assert.True(result.Succeeded);
        Assert.NotNull(helper.Goal);
        Assert.Equal(9, helper.Goal!.DestinationX);
        Assert.Equal(5, helper.Goal.DestinationY);
    }

    [Fact]
    public async Task BotReply_CreatesScriptedResponse()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomEntryService roomEntryService = services.GetRequiredService<IRoomEntryService>();
        IRoomInteractionService roomInteractionService = services.GetRequiredService<IRoomInteractionService>();
        IRoomRuntimeRepository roomRuntimeRepository = services.GetRequiredService<IRoomRuntimeRepository>();

        await roomEntryService.EnterAsync(
            new RoomEntryRequest(new CharacterId(7), new RoomId(1), null, false),
            cancellationToken);
        await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(1), new RoomId(1), ":botcreate Helper"),
            cancellationToken);
        await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(1), new RoomId(1), ":botreply Helper zzzbottrigger Hello from helper."),
            cancellationToken);

        RoomChatResult result = await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(7), new RoomId(1), "zzzbottrigger"),
            cancellationToken);

        IReadOnlyList<RoomChatMessage> messages =
            await roomRuntimeRepository.GetChatMessagesByRoomIdAsync(new RoomId(1), cancellationToken);

        Assert.True(result.Succeeded);
        Assert.Contains(messages, message =>
            string.Equals(message.SenderName, "Helper", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(message.Message, "Hello from helper.", StringComparison.Ordinal));
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
