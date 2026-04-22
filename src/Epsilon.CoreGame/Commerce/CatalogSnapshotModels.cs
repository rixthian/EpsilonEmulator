using Epsilon.Content;

namespace Epsilon.CoreGame;

public sealed record CatalogOfferSnapshot(
    CatalogOfferDefinition Offer,
    IReadOnlyList<string> ProductNames,
    int TotalProductCount);

public sealed record CatalogPageSnapshot(
    CatalogPageDefinition Page,
    IReadOnlyList<CatalogOfferSnapshot> Offers);
