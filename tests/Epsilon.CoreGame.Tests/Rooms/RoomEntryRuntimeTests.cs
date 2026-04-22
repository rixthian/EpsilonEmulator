using Epsilon.Games;
using Epsilon.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Epsilon.CoreGame.Tests;

public sealed class RoomEntryRuntimeTests
{
    [Fact]
    public async Task EnterAsync_RegistersActorPresenceInTargetRoom()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomEntryService roomEntryService = services.GetRequiredService<IRoomEntryService>();
        IRoomRuntimeRepository roomRuntimeRepository = services.GetRequiredService<IRoomRuntimeRepository>();
        IRoomRuntimeCoordinator roomRuntimeCoordinator = services.GetRequiredService<IRoomRuntimeCoordinator>();

        RoomEntryResult result = await roomEntryService.EnterAsync(
            new RoomEntryRequest(new CharacterId(1), new RoomId(10), null, false),
            cancellationToken);

        RoomActorState? actor = await roomRuntimeRepository.GetActorByIdAsync(new RoomId(10), 1, cancellationToken);
        RoomRuntimeCoordinationSnapshot? snapshot =
            await roomRuntimeCoordinator.GetSnapshotAsync(new RoomId(10), cancellationToken);

        Assert.True(result.Succeeded);
        Assert.NotNull(actor);
        Assert.NotNull(snapshot);
        Assert.Equal(RoomRuntimeMutationKind.ActorPresenceChanged, snapshot!.LastMutationKind);
        Assert.Contains(
            result.Stages,
            stage => stage.Stage == RoomEntryStage.RuntimePresenceRegistered && stage.Succeeded);
    }

    [Fact]
    public async Task EnterAsync_MigratesActorPresenceFromPreviousRoom()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomEntryService roomEntryService = services.GetRequiredService<IRoomEntryService>();
        IRoomRuntimeRepository roomRuntimeRepository = services.GetRequiredService<IRoomRuntimeRepository>();
        IRoomRuntimeCoordinator roomRuntimeCoordinator = services.GetRequiredService<IRoomRuntimeCoordinator>();

        RoomEntryResult result = await roomEntryService.EnterAsync(
            new RoomEntryRequest(new CharacterId(1), new RoomId(11), null, false),
            cancellationToken);

        RoomActorState? previousRoomActor =
            await roomRuntimeRepository.GetActorByIdAsync(new RoomId(1), 1, cancellationToken);
        RoomActorState? targetRoomActor =
            await roomRuntimeRepository.GetActorByIdAsync(new RoomId(11), 1, cancellationToken);
        RoomRuntimeCoordinationSnapshot? previousRoomSnapshot =
            await roomRuntimeCoordinator.GetSnapshotAsync(new RoomId(1), cancellationToken);

        Assert.True(result.Succeeded);
        Assert.Null(previousRoomActor);
        Assert.NotNull(targetRoomActor);
        Assert.NotNull(previousRoomSnapshot);
        Assert.Equal(RoomRuntimeMutationKind.ActorPresenceChanged, previousRoomSnapshot!.LastMutationKind);
    }

    [Fact]
    public async Task EnterAsync_PublicRoomMaterializesConfiguredBots()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomEntryService roomEntryService = services.GetRequiredService<IRoomEntryService>();
        IRoomRuntimeRepository roomRuntimeRepository = services.GetRequiredService<IRoomRuntimeRepository>();

        RoomEntryResult result = await roomEntryService.EnterAsync(
            new RoomEntryRequest(new CharacterId(7), new RoomId(1), null, false),
            cancellationToken);

        IReadOnlyList<RoomActorState> actors =
            await roomRuntimeRepository.GetActorsByRoomIdAsync(new RoomId(1), cancellationToken);

        Assert.True(result.Succeeded);
        Assert.Contains(actors, actor => actor.ActorKind == RoomActorKind.Bot && actor.DisplayName == "Concierge");
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
