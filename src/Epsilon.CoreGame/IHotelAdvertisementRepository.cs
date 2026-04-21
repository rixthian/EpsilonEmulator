namespace Epsilon.CoreGame;

public interface IHotelAdvertisementRepository
{
    ValueTask<IReadOnlyList<HotelAdvertisement>> GetActiveByPlacementAsync(
        string placementKey,
        CancellationToken cancellationToken = default);
}
