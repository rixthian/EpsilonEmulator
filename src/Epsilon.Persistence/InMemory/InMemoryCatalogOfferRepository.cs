using Epsilon.Content;
using Epsilon.CoreGame;

namespace Epsilon.Persistence;

internal sealed class InMemoryCatalogOfferRepository : ICatalogOfferRepository
{
    private readonly InMemoryHotelStore _store;

    public InMemoryCatalogOfferRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<IReadOnlyList<CatalogOfferDefinition>> GetByPageIdAsync(
        CatalogPageId catalogPageId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CatalogOfferDefinition> offers = _store.CatalogOffers
            .Where(candidate => candidate.CatalogPageId == catalogPageId)
            .OrderBy(candidate => candidate.CatalogName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return ValueTask.FromResult(offers);
    }

    public ValueTask<CatalogOfferDefinition?> GetByIdAsync(
        CatalogOfferId catalogOfferId,
        CancellationToken cancellationToken = default)
    {
        CatalogOfferDefinition? offer = _store.CatalogOffers
            .FirstOrDefault(candidate => candidate.CatalogOfferId == catalogOfferId);

        return ValueTask.FromResult(offer);
    }
}
