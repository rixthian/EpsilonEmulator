using Epsilon.CoreGame;

namespace Epsilon.Content;

public interface ICatalogOfferRepository
{
    ValueTask<IReadOnlyList<CatalogOfferDefinition>> GetByPageIdAsync(
        CatalogPageId catalogPageId,
        CancellationToken cancellationToken = default);

    ValueTask<CatalogOfferDefinition?> GetByIdAsync(
        CatalogOfferId catalogOfferId,
        CancellationToken cancellationToken = default);
}
