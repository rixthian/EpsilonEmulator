namespace Epsilon.CoreGame;

public interface IHotelCommerceService
{
    ValueTask<IReadOnlyList<CatalogPageSnapshot>> GetCatalogPagesAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);

    ValueTask<CatalogPageSnapshot?> GetCatalogPageAsync(
        CharacterId characterId,
        CatalogPageId catalogPageId,
        CancellationToken cancellationToken = default);

    ValueTask<InventorySnapshot> GetInventoryAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);

    ValueTask<CatalogPurchaseResult> PurchaseAsync(
        CatalogPurchaseRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<RedeemVoucherResult> RedeemVoucherAsync(
        RedeemVoucherRequest request,
        CancellationToken cancellationToken = default);
}
