using Epsilon.CoreGame;

namespace Epsilon.Content;

public sealed record CatalogOfferDefinition(
    CatalogOfferId CatalogOfferId,
    CatalogPageId CatalogPageId,
    string CatalogName,
    CatalogOfferKind OfferKind,
    int CreditsCost,
    int ActivityPointsCost,
    int SnowCost,
    IReadOnlyList<CatalogOfferProductDefinition> Products);
