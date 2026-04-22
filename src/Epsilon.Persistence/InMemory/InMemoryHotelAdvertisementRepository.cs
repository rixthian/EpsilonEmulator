using Epsilon.CoreGame;

namespace Epsilon.Persistence;

internal sealed class InMemoryHotelAdvertisementRepository : IHotelAdvertisementRepository
{
    private readonly InMemoryHotelStore _store;

    public InMemoryHotelAdvertisementRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<IReadOnlyList<HotelAdvertisement>> GetActiveByPlacementAsync(
        string placementKey,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<HotelAdvertisement> advertisements = _store.Advertisements
            .Where(advertisement =>
                advertisement.IsActive &&
                string.Equals(advertisement.PlacementKey, placementKey, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return ValueTask.FromResult(advertisements);
    }
}
