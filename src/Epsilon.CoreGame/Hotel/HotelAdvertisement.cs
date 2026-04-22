namespace Epsilon.CoreGame;

public sealed record HotelAdvertisement(
    long AdvertisementId,
    string PlacementKey,
    string ImageAssetPath,
    string ClickThroughUrl,
    int ViewCount,
    int ViewLimit,
    bool IsActive)
{
    public bool HasRemainingCapacity => ViewLimit <= 0 || ViewCount < ViewLimit;
}
