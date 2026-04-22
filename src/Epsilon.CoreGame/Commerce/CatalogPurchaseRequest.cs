namespace Epsilon.CoreGame;

public sealed record CatalogPurchaseRequest(
    CharacterId CharacterId,
    CatalogOfferId CatalogOfferId);
