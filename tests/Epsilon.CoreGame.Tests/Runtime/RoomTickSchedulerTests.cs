using Epsilon.Games;
using Epsilon.Persistence;
using Epsilon.Rooms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Epsilon.CoreGame.Tests;

public sealed class RoomTickSchedulerTests
{
    [Fact]
    public async Task TickAsync_SlidesActorStandingOnRoller()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomTickScheduler roomTickScheduler = services.GetRequiredService<IRoomTickScheduler>();
        IRoomRuntimeRepository roomRuntimeRepository = services.GetRequiredService<IRoomRuntimeRepository>();
        IRoomRuntimeCoordinator roomRuntimeCoordinator = services.GetRequiredService<IRoomRuntimeCoordinator>();
        IRoomItemRepository roomItemRepository = services.GetRequiredService<IRoomItemRepository>();

        RoomId roomId = new(1);
        RoomActorState actor = (await roomRuntimeRepository.GetActorByIdAsync(roomId, 1, cancellationToken))!;
        await roomRuntimeRepository.StoreActorStateAsync(
            roomId,
            actor with { Position = new RoomCoordinate(9, 6, 0), Goal = null, IsWalking = false },
            cancellationToken);

        List<RoomItemState> items = (await roomItemRepository.GetByRoomIdAsync(roomId, cancellationToken)).ToList();
        items.Add(new RoomItemState(
            new ItemId(9001),
            new ItemDefinitionId(1099),
            roomId,
            new RoomItemPlacement(new FloorPosition(9, 6, 0), 2, null),
            ""));
        await roomItemRepository.StoreByRoomIdAsync(roomId, items, cancellationToken);

        int mutations = await roomTickScheduler.TickAsync(1, cancellationToken);
        RoomActorState? movedActor = await roomRuntimeRepository.GetActorByIdAsync(roomId, 1, cancellationToken);
        RoomRuntimeCoordinationSnapshot? snapshot = await roomRuntimeCoordinator.GetSnapshotAsync(roomId, cancellationToken);

        Assert.True(mutations >= 1);
        Assert.NotNull(movedActor);
        Assert.Equal(10, movedActor!.Position.X);
        Assert.Equal(6, movedActor.Position.Y);
        Assert.NotNull(snapshot);
        Assert.Equal(RoomRuntimeMutationKind.ActorStateChanged, snapshot!.LastMutationKind);
    }

    [Fact]
    public async Task TickAsync_SlidesTopItemStandingOnRoller()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomTickScheduler roomTickScheduler = services.GetRequiredService<IRoomTickScheduler>();
        IRoomRuntimeRepository roomRuntimeRepository = services.GetRequiredService<IRoomRuntimeRepository>();
        IRoomRuntimeCoordinator roomRuntimeCoordinator = services.GetRequiredService<IRoomRuntimeCoordinator>();
        IRoomItemRepository roomItemRepository = services.GetRequiredService<IRoomItemRepository>();

        RoomId roomId = new(1);
        RoomActorState actor = (await roomRuntimeRepository.GetActorByIdAsync(roomId, 1, cancellationToken))!;
        await roomRuntimeRepository.StoreActorStateAsync(
            roomId,
            actor with { Position = new RoomCoordinate(5, 5, 0), Goal = null, IsWalking = false },
            cancellationToken);

        List<RoomItemState> items = (await roomItemRepository.GetByRoomIdAsync(roomId, cancellationToken)).ToList();
        items.Add(new RoomItemState(
            new ItemId(9002),
            new ItemDefinitionId(1099),
            roomId,
            new RoomItemPlacement(new FloorPosition(9, 6, 0), 2, null),
            ""));
        items.Add(new RoomItemState(
            new ItemId(9003),
            new ItemDefinitionId(1003),
            roomId,
            new RoomItemPlacement(new FloorPosition(9, 6, 1), 0, null),
            ""));
        await roomItemRepository.StoreByRoomIdAsync(roomId, items, cancellationToken);

        int mutations = await roomTickScheduler.TickAsync(1, cancellationToken);
        IReadOnlyList<RoomItemState> updatedItems = await roomItemRepository.GetByRoomIdAsync(roomId, cancellationToken);
        RoomItemState movedItem = Assert.Single(updatedItems, item => item.ItemId == new ItemId(9003));
        RoomRuntimeCoordinationSnapshot? snapshot = await roomRuntimeCoordinator.GetSnapshotAsync(roomId, cancellationToken);

        Assert.True(mutations >= 1);
        Assert.NotNull(movedItem.Placement.FloorPosition);
        Assert.Equal(10, movedItem.Placement.FloorPosition!.Value.X);
        Assert.Equal(6, movedItem.Placement.FloorPosition!.Value.Y);
        Assert.NotNull(snapshot);
        Assert.Equal(RoomRuntimeMutationKind.RoomContentChanged, snapshot!.LastMutationKind);
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
