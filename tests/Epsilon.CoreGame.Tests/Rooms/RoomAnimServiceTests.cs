using Epsilon.Games;
using Epsilon.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Epsilon.CoreGame.Tests;

public sealed class RoomAnimServiceTests
{
    [Fact]
    public async Task BuildAsync_MapsActorDanceAnimation()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomInteractionService roomInteractionService = services.GetRequiredService<IRoomInteractionService>();
        IRoomAnimService roomAnimService = services.GetRequiredService<IRoomAnimService>();

        await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(1), new RoomId(1), ":dance 3"),
            cancellationToken);

        RoomAnimSnapshot? snapshot = await roomAnimService.BuildAsync(new RoomId(1), cancellationToken);

        Assert.NotNull(snapshot);
        ActorAnimState actor = Assert.Single(snapshot!.Actors, candidate =>
            candidate.ActorKind == RoomActorKind.Player &&
            string.Equals(candidate.DisplayName, "epsilon", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("dance", actor.AnimationKey);
        Assert.Equal("3", actor.VariantKey);
    }

    [Fact]
    public async Task BuildAsync_MapsCarryLayerAndDropTransition()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomInteractionService roomInteractionService = services.GetRequiredService<IRoomInteractionService>();
        IRoomAnimService roomAnimService = services.GetRequiredService<IRoomAnimService>();

        RoomAnimSnapshot? beforeDrop = await roomAnimService.BuildAsync(new RoomId(1), cancellationToken);
        await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(1), new RoomId(1), ":drop"),
            cancellationToken);
        RoomAnimSnapshot? afterDrop = await roomAnimService.BuildAsync(new RoomId(1), cancellationToken);

        Assert.NotNull(beforeDrop);
        Assert.NotNull(afterDrop);
        ActorAnimState actorBeforeDrop = Assert.Single(beforeDrop!.Actors, candidate =>
            candidate.ActorKind == RoomActorKind.Player &&
            string.Equals(candidate.DisplayName, "epsilon", StringComparison.OrdinalIgnoreCase));
        ActorAnimState actorAfterDrop = Assert.Single(afterDrop!.Actors, candidate =>
            candidate.ActorKind == RoomActorKind.Player &&
            string.Equals(candidate.DisplayName, "epsilon", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("carry", actorBeforeDrop.ActiveLayerKeys);
        Assert.DoesNotContain("carry", actorAfterDrop.ActiveLayerKeys);
    }

    [Fact]
    public async Task BuildAsync_MapsAnimatedTeleportFurni()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomAnimService roomAnimService = services.GetRequiredService<IRoomAnimService>();

        RoomAnimSnapshot? snapshot = await roomAnimService.BuildAsync(new RoomId(1), cancellationToken);

        Assert.NotNull(snapshot);
        Assert.Contains(snapshot!.Items, item =>
            string.Equals(item.InteractionTypeCode, "teleport", StringComparison.OrdinalIgnoreCase) &&
            item.IsAnimated &&
            string.Equals(item.AnimationKey, "idle", StringComparison.OrdinalIgnoreCase));
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
