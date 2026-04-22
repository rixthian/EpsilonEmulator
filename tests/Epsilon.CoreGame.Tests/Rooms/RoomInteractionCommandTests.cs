using Epsilon.Games;
using Epsilon.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Epsilon.CoreGame.Tests;

public sealed class RoomInteractionCommandTests
{
    [Fact]
    public async Task PickAll_MovesRoomItemsIntoInventoryAndSignalsRoomContentChange()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomInteractionService roomInteractionService = services.GetRequiredService<IRoomInteractionService>();
        IInventoryRepository inventoryRepository = services.GetRequiredService<IInventoryRepository>();
        Epsilon.Rooms.IRoomItemRepository roomItemRepository = services.GetRequiredService<Epsilon.Rooms.IRoomItemRepository>();
        IRoomRuntimeCoordinator roomRuntimeCoordinator = services.GetRequiredService<IRoomRuntimeCoordinator>();

        IReadOnlyList<InventoryItemState> inventoryBefore =
            await inventoryRepository.GetByCharacterIdAsync(new CharacterId(1), cancellationToken);

        RoomChatResult result = await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(1), new RoomId(1), ":pickall"),
            cancellationToken);

        IReadOnlyList<InventoryItemState> inventoryAfter =
            await inventoryRepository.GetByCharacterIdAsync(new CharacterId(1), cancellationToken);
        IReadOnlyList<Epsilon.Rooms.RoomItemState> roomItemsAfter =
            await roomItemRepository.GetByRoomIdAsync(new RoomId(1), cancellationToken);
        RoomRuntimeCoordinationSnapshot? snapshot =
            await roomRuntimeCoordinator.GetSnapshotAsync(new RoomId(1), cancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal(inventoryBefore.Count + 2, inventoryAfter.Count);
        Assert.Empty(roomItemsAfter);
        Assert.NotNull(snapshot);
        Assert.Equal(RoomRuntimeMutationKind.RoomContentChanged, snapshot!.LastMutationKind);
        Assert.Contains("2 room item(s)", result.Detail);
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
