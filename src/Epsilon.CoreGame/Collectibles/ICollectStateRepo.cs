namespace Epsilon.CoreGame;

public interface ICollectStateRepo
{
    ValueTask<IReadOnlyList<string>> GetPendingGiftBoxesAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);

    ValueTask<bool> RemovePendingGiftBoxAsync(
        CharacterId characterId,
        string boxKey,
        CancellationToken cancellationToken = default);

    ValueTask<DateTime?> GetFactoryClaimAsync(
        CharacterId characterId,
        string factoryKey,
        CancellationToken cancellationToken = default);

    ValueTask StoreFactoryClaimAsync(
        CharacterId characterId,
        string factoryKey,
        DateTime claimedAtUtc,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<MarketListingState>> GetMarketListingsAsync(
        CancellationToken cancellationToken = default);

    ValueTask<long> ReserveMarketListingIdAsync(
        CancellationToken cancellationToken = default);

    ValueTask StoreMarketListingAsync(
        MarketListingState listing,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically marks a listing as inactive only if it is currently active.
    /// Returns the deactivated listing on success, or null if the listing was
    /// not found or was already inactive (i.e. another buyer won the race).
    /// </summary>
    ValueTask<MarketListingState?> TryDeactivateListingAsync(
        long listingId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically records a factory claim only if no claim exists or the
    /// cooldown has elapsed since the last claim.
    /// Returns the claim timestamp on success, or null if the factory is not ready.
    /// </summary>
    ValueTask<DateTime?> TryClaimFactoryAsync(
        CharacterId characterId,
        string factoryKey,
        TimeSpan cooldown,
        DateTime now,
        CancellationToken cancellationToken = default);
}
