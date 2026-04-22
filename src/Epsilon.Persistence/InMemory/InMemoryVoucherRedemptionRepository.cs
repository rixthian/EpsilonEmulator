using Epsilon.CoreGame;

namespace Epsilon.Persistence;

internal sealed class InMemoryVoucherRedemptionRepository : IVoucherRedemptionRepository
{
    private readonly InMemoryHotelStore _store;
    private readonly object _syncRoot = new();

    public InMemoryVoucherRedemptionRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<bool> TryRedeemAsync(
        CharacterId characterId,
        string voucherCode,
        CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            if (!_store.RedeemedVoucherCodes.TryGetValue(characterId, out HashSet<string>? redeemedCodes))
            {
                redeemedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                _store.RedeemedVoucherCodes[characterId] = redeemedCodes;
            }

            return ValueTask.FromResult(redeemedCodes.Add(voucherCode));
        }
    }
}
