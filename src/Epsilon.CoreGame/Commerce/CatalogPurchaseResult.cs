namespace Epsilon.CoreGame;

public sealed record CatalogPurchaseResult(
    bool Succeeded,
    string Detail,
    CatalogOfferId? CatalogOfferId,
    InventorySnapshot? Inventory,
    WalletSnapshot? Wallet);
