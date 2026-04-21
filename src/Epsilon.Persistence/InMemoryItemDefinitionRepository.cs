using Epsilon.Content;
using Epsilon.CoreGame;

namespace Epsilon.Persistence;

internal sealed class InMemoryItemDefinitionRepository : IItemDefinitionRepository
{
    private readonly InMemoryHotelStore _store;

    public InMemoryItemDefinitionRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<ItemDefinition?> GetByIdAsync(ItemDefinitionId itemDefinitionId, CancellationToken cancellationToken = default)
    {
        _store.ItemDefinitions.TryGetValue(itemDefinitionId, out ItemDefinition? definition);
        return ValueTask.FromResult(definition);
    }
}

