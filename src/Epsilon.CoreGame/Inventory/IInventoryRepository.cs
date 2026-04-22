namespace Epsilon.CoreGame;

public interface IInventoryRepository
{
    ValueTask<IReadOnlyList<InventoryItemState>> GetByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);

    ValueTask AddItemsAsync(
        CharacterId characterId,
        IReadOnlyList<ItemDefinitionId> itemDefinitionIds,
        string reasonCode,
        CancellationToken cancellationToken = default);

    ValueTask AddExistingItemsAsync(
        CharacterId characterId,
        IReadOnlyList<InventoryItemState> items,
        CancellationToken cancellationToken = default);
}
