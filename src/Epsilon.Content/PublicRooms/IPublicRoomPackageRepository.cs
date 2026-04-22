namespace Epsilon.Content;

public interface IPublicRoomPackageRepository
{
    ValueTask<PublicRoomPackageDefinition?> GetByKeyAsync(
        string assetPackageKey,
        CancellationToken cancellationToken = default);
}
