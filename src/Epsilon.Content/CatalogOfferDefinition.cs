using Epsilon.CoreGame;

namespace Epsilon.Content;

public sealed record CatalogOfferDefinition(
    CatalogOfferId CatalogOfferId,
    CatalogPageId CatalogPageId,
    string CatalogName,
    int CreditsCost,
    int ActivityPointsCost,
    int SnowCost,
    int Amount,
    IReadOnlyList<ItemDefinitionId> ItemDefinitionIds);

