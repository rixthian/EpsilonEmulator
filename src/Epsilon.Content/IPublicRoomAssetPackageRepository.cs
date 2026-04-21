namespace Epsilon.Content;

public interface IPublicRoomAssetPackageRepository
{
    ValueTask<PublicRoomAssetPackageDefinition?> GetByKeyAsync(
        string assetPackageKey,
        CancellationToken cancellationToken = default);
}
