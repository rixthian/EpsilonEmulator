namespace Epsilon.Rooms;

public interface IPublicRoomBehaviorRepository
{
    ValueTask<IReadOnlyList<PublicRoomBehaviorDefinition>> GetByAssetPackageKeyAsync(
        string assetPackageKey,
        CancellationToken cancellationToken = default);
}
