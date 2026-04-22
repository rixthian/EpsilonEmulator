using Epsilon.CoreGame;

namespace Epsilon.Persistence;

internal sealed class InMemoryInventoryRepository : IInventoryRepository
{
    private readonly InMemoryHotelStore _store;
    private readonly object _syncRoot = new();

    public InMemoryInventoryRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<IReadOnlyList<InventoryItemState>> GetByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            IReadOnlyList<InventoryItemState> items = _store.InventoryItems.TryGetValue(characterId, out List<InventoryItemState>? inventory)
                ? inventory.OrderByDescending(candidate => candidate.CreatedAtUtc).ToArray()
                : [];

            return ValueTask.FromResult(items);
        }
    }

    public ValueTask AddItemsAsync(
        CharacterId characterId,
        IReadOnlyList<ItemDefinitionId> itemDefinitionIds,
        string reasonCode,
        CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            if (!_store.InventoryItems.TryGetValue(characterId, out List<InventoryItemState>? inventory))
            {
                inventory = [];
                _store.InventoryItems[characterId] = inventory;
            }

            foreach (ItemDefinitionId itemDefinitionId in itemDefinitionIds)
            {
                InventoryItemState item = new(
                    new ItemId(_store.NextItemId++),
                    characterId,
                    itemDefinitionId,
                    reasonCode,
                    DateTime.UtcNow);

                inventory.Add(item);
            }

            return ValueTask.CompletedTask;
        }
    }

    public ValueTask AddExistingItemsAsync(
        CharacterId characterId,
        IReadOnlyList<InventoryItemState> items,
        CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            if (!_store.InventoryItems.TryGetValue(characterId, out List<InventoryItemState>? inventory))
            {
                inventory = [];
                _store.InventoryItems[characterId] = inventory;
            }

            foreach (InventoryItemState item in items)
            {
                inventory.Add(item with
                {
                    OwnerCharacterId = characterId
                });
            }

            return ValueTask.CompletedTask;
        }
    }
}
