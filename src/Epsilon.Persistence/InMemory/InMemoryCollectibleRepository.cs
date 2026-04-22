using Epsilon.Content;

namespace Epsilon.Persistence;

internal sealed class InMemoryCollectibleRepository : ICollectibleRepository
{
    private readonly InMemoryHotelStore _store;

    public InMemoryCollectibleRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<IReadOnlyList<CollectibleDefinition>> GetVisibleAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CollectibleDefinition> collectibles = _store.CollectibleDefinitions
            .Where(candidate => candidate.IsVisible)
            .OrderBy(candidate => candidate.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return ValueTask.FromResult(collectibles);
    }
}
