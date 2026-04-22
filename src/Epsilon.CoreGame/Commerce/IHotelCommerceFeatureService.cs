namespace Epsilon.CoreGame;

public interface IHotelCommerceFeatureService
{
    ValueTask<HotelCommerceFeatureSnapshot> GetSnapshotAsync(
        CancellationToken cancellationToken = default);
}
