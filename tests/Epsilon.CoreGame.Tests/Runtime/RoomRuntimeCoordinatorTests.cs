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
    public async Task MoveActor_RejectsLongRangeTeleportStep()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomInteractionService roomInteractionService = services.GetRequiredService<IRoomInteractionService>();

        RoomActorMovementResult result = await roomInteractionService.MoveActorAsync(
            new RoomActorMovementRequest(new CharacterId(1), new RoomId(1), 9, 9),
            cancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains("out of step range", result.Detail, StringComparison.OrdinalIgnoreCase);
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
