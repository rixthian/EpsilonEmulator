using Epsilon.Content;

namespace Epsilon.Persistence;

internal sealed class InMemoryEffectDefinitionRepository : IEffectDefinitionRepository
{
    private readonly InMemoryHotelStore _store;

    public InMemoryEffectDefinitionRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<IReadOnlyList<EffectDefinition>> GetVisibleAsync(
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<IReadOnlyList<EffectDefinition>>(_store.EffectDefinitions);
    }
}
