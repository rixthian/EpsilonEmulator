using Epsilon.Content;
using Epsilon.CoreGame;

namespace Epsilon.Persistence;

internal sealed class InMemoryCatalogPageRepository : ICatalogPageRepository
{
    private readonly InMemoryHotelStore _store;

    public InMemoryCatalogPageRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<IReadOnlyList<CatalogPageDefinition>> GetVisibleByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CatalogPageDefinition> pages = _store.CatalogPages
            .Where(page => page.IsVisible && page.IsEnabled)
            .OrderBy(page => page.OrderNumber)
            .ToArray();

        return ValueTask.FromResult(pages);
    }

    public ValueTask<CatalogPageDefinition?> GetByIdAsync(
        CatalogPageId catalogPageId,
        CancellationToken cancellationToken = default)
    {
        CatalogPageDefinition? page = _store.CatalogPages
            .FirstOrDefault(candidate => candidate.CatalogPageId == catalogPageId);

        return ValueTask.FromResult(page);
    }
}
