using Epsilon.Games;
using Epsilon.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Epsilon.CoreGame.Tests;

public sealed class RoomRuntimeCoordinatorTests
{
    [Fact]
    public async Task SendChat_UpdatesRoomRuntimeCoordinationSnapshot()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomInteractionService roomInteractionService = services.GetRequiredService<IRoomInteractionService>();
        IRoomRuntimeCoordinator roomRuntimeCoordinator = services.GetRequiredService<IRoomRuntimeCoordinator>();

        RoomChatResult result = await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(1), new RoomId(1), "runtime signal check"),
            cancellationToken);

        RoomRuntimeCoordinationSnapshot? snapshot =
            await roomRuntimeCoordinator.GetSnapshotAsync(new RoomId(1), cancellationToken);

        Assert.True(result.Succeeded);
        Assert.NotNull(snapshot);
        Assert.Equal(RoomRuntimeMutationKind.ChatMessageAppended, snapshot!.LastMutationKind);
        Assert.True(snapshot.Version >= 1);
        Assert.True(snapshot.ActorCount >= 1);
        Assert.True(snapshot.PlayerCount >= 1);
    }

    [Fact]
    public async Task MoveActor_UpdatesRoomRuntimeCoordinationSnapshot()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomInteractionService roomInteractionService = services.GetRequiredService<IRoomInteractionService>();
        IRoomRuntimeCoordinator roomRuntimeCoordinator = services.GetRequiredService<IRoomRuntimeCoordinator>();

        RoomActorMovementResult result = await roomInteractionService.MoveActorAsync(
            new RoomActorMovementRequest(new CharacterId(1), new RoomId(1), 6, 5),
            cancellationToken);

        RoomRuntimeCoordinationSnapshot? snapshot =
            await roomRuntimeCoordinator.GetSnapshotAsync(new RoomId(1), cancellationToken);

        Assert.True(result.Succeeded);
        Assert.NotNull(snapshot);
        Assert.Equal(RoomRuntimeMutationKind.ActorStateChanged, snapshot!.LastMutationKind);
        Assert.True(snapshot.Version >= 1);
        Assert.True(snapshot.ActorCount >= 1);
        Assert.True(snapshot.PlayerCount >= 1);
    }

    [Fact]
    public async Task MoveActor_AllowsReachableLongRangeDestination()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomInteractionService roomInteractionService = services.GetRequiredService<IRoomInteractionService>();
        IRoomRuntimeRepository roomRuntimeRepository = services.GetRequiredService<IRoomRuntimeRepository>();

        RoomActorMovementResult result = await roomInteractionService.MoveActorAsync(
            new RoomActorMovementRequest(new CharacterId(1), new RoomId(1), 10, 6),
            cancellationToken);

        RoomActorState? actor = await roomRuntimeRepository.GetActorByIdAsync(new RoomId(1), 1, cancellationToken);

        Assert.True(result.Succeeded);
        Assert.NotNull(actor);
        Assert.Equal(10, actor!.Position.X);
        Assert.Equal(6, actor.Position.Y);
    }

    [Fact]
    public async Task MoveActor_CompletesImmediatelyAndClearsTransientWalkingState()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomInteractionService roomInteractionService = services.GetRequiredService<IRoomInteractionService>();
        IRoomRuntimeRepository roomRuntimeRepository = services.GetRequiredService<IRoomRuntimeRepository>();

        RoomActorMovementResult result = await roomInteractionService.MoveActorAsync(
            new RoomActorMovementRequest(new CharacterId(1), new RoomId(1), 10, 6),
            cancellationToken);

        RoomActorState? actor = await roomRuntimeRepository.GetActorByIdAsync(new RoomId(1), 1, cancellationToken);

        Assert.True(result.Succeeded);
        Assert.NotNull(actor);
        Assert.False(actor!.IsWalking);
        Assert.Null(actor.Goal);
        Assert.DoesNotContain(actor.StatusEntries, entry => entry.Key == "mv");
    }

    [Fact]
    public async Task RoomEntry_UpdatesOccupancyCounts()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomEntryService roomEntryService = services.GetRequiredService<IRoomEntryService>();
        IRoomRuntimeCoordinator roomRuntimeCoordinator = services.GetRequiredService<IRoomRuntimeCoordinator>();

        RoomEntryResult result = await roomEntryService.EnterAsync(
            new RoomEntryRequest(new CharacterId(7), new RoomId(1), null, false),
            cancellationToken);

        RoomRuntimeCoordinationSnapshot? snapshot =
            await roomRuntimeCoordinator.GetSnapshotAsync(new RoomId(1), cancellationToken);

        Assert.True(result.Succeeded);
        Assert.NotNull(snapshot);
        Assert.Equal(RoomRuntimeMutationKind.ActorPresenceChanged, snapshot!.LastMutationKind);
        Assert.True(snapshot.ActorCount >= 2);
        Assert.True(snapshot.PlayerCount >= 2);
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
