namespace Epsilon.CoreGame;

public interface ICollectFeatService
{
    ValueTask<CollectorProgressSnapshot> GetProgressAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);

    ValueTask<CollectorProgressSnapshot> GrantXpAsync(
        CharacterId characterId,
        int xp,
        string reasonCode,
        CancellationToken cancellationToken = default);

    ValueTask<WalletSnapshot?> AccrueEmeraldsAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<GiftBoxSnapshot>> GetGiftBoxesAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);

    ValueTask<GiftBoxOpenResult?> OpenGiftBoxAsync(
        CharacterId characterId,
        string boxKey,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<FactorySnapshot>> GetFactoriesAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);

    ValueTask<GiftBoxOpenResult?> ClaimFactoryAsync(
        CharacterId characterId,
        string factoryKey,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<CollectRecipeSnapshot>> GetRecipesAsync(
        CancellationToken cancellationToken = default);

    ValueTask<GiftBoxOpenResult?> RecycleAsync(
        CharacterId characterId,
        string recipeKey,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<MarketListingSnapshot>> GetMarketListingsAsync(
        CancellationToken cancellationToken = default);

    ValueTask<MarketListingSnapshot?> CreateListingAsync(
        CharacterId characterId,
        string collectibleKey,
        int priceEmeralds,
        CancellationToken cancellationToken = default);

    ValueTask<MarketListingSnapshot?> BuyListingAsync(
        CharacterId characterId,
        long listingId,
        CancellationToken cancellationToken = default);

    ValueTask<CollectiblesPublicSnapshot> GetPublicSnapshotAsync(
        CancellationToken cancellationToken = default);
}
