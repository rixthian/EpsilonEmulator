using Epsilon.CoreGame;

namespace Epsilon.Persistence;

internal sealed class InMemoryCollectStateRepo : ICollectStateRepo
{
    private readonly InMemoryHotelStore _store;
    private readonly object _sync = new();

    public InMemoryCollectStateRepo(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<IReadOnlyList<string>> GetPendingGiftBoxesAsync(CharacterId characterId, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            IReadOnlyList<string> values = _store.PendingGiftBoxes.TryGetValue(characterId, out List<string>? boxes)
                ? boxes.ToArray()
                : [];
            return ValueTask.FromResult(values);
        }
    }

    public ValueTask<bool> RemovePendingGiftBoxAsync(CharacterId characterId, string boxKey, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (_store.PendingGiftBoxes.TryGetValue(characterId, out List<string>? boxes))
            {
                return ValueTask.FromResult(boxes.Remove(boxKey));
            }

            return ValueTask.FromResult(false);
        }
    }

    public ValueTask<DateTime?> GetFactoryClaimAsync(CharacterId characterId, string factoryKey, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (_store.FactoryClaims.TryGetValue(characterId, out Dictionary<string, DateTime>? claims) &&
                claims.TryGetValue(factoryKey, out DateTime claimedAtUtc))
            {
                return ValueTask.FromResult<DateTime?>(claimedAtUtc);
            }

            return ValueTask.FromResult<DateTime?>(null);
        }
    }

    public ValueTask StoreFactoryClaimAsync(CharacterId characterId, string factoryKey, DateTime claimedAtUtc, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (!_store.FactoryClaims.TryGetValue(characterId, out Dictionary<string, DateTime>? claims))
            {
                claims = new(StringComparer.OrdinalIgnoreCase);
                _store.FactoryClaims[characterId] = claims;
            }

            claims[factoryKey] = claimedAtUtc;
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask<IReadOnlyList<MarketListingState>> GetMarketListingsAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return ValueTask.FromResult<IReadOnlyList<MarketListingState>>(_store.MarketListings.ToArray());
        }
    }

    public ValueTask<long> ReserveMarketListingIdAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return ValueTask.FromResult(_store.NextMarketListingId++);
        }
    }

    public ValueTask StoreMarketListingAsync(MarketListingState listing, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            int index = _store.MarketListings.FindIndex(candidate => candidate.ListingId == listing.ListingId);
            if (index >= 0)
            {
                _store.MarketListings[index] = listing;
            }
            else
            {
                _store.MarketListings.Add(listing);
            }
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask<MarketListingState?> TryDeactivateListingAsync(long listingId, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            int index = _store.MarketListings.FindIndex(candidate => candidate.ListingId == listingId);
            if (index < 0)
            {
                return ValueTask.FromResult<MarketListingState?>(null);
            }

            MarketListingState listing = _store.MarketListings[index];
            if (!listing.IsActive)
            {
                return ValueTask.FromResult<MarketListingState?>(null);
            }

            MarketListingState deactivated = listing with { IsActive = false };
            _store.MarketListings[index] = deactivated;
            return ValueTask.FromResult<MarketListingState?>(deactivated);
        }
    }

    public ValueTask<DateTime?> TryClaimFactoryAsync(
        CharacterId characterId,
        string factoryKey,
        TimeSpan cooldown,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            DateTime? lastClaim = null;
            if (_store.FactoryClaims.TryGetValue(characterId, out Dictionary<string, DateTime>? claims) &&
                claims.TryGetValue(factoryKey, out DateTime stored))
            {
                lastClaim = stored;
            }

            if (lastClaim is not null && lastClaim.Value + cooldown > now)
            {
                return ValueTask.FromResult<DateTime?>(null);
            }

            if (claims is null)
            {
                claims = new(StringComparer.OrdinalIgnoreCase);
                _store.FactoryClaims[characterId] = claims;
            }

            claims[factoryKey] = now;
            return ValueTask.FromResult<DateTime?>(now);
        }
    }
}
