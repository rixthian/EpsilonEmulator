using Epsilon.Content;

namespace Epsilon.Persistence;

internal sealed class InMemoryPublicRoomAssetPackageRepository : IPublicRoomAssetPackageRepository
{
    private readonly InMemoryHotelStore _store;

    public InMemoryPublicRoomAssetPackageRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<PublicRoomAssetPackageDefinition?> GetByKeyAsync(
        string assetPackageKey,
        CancellationToken cancellationToken = default)
    {
        _store.PublicRoomAssetPackages.TryGetValue(assetPackageKey, out PublicRoomAssetPackageDefinition? definition);
        return ValueTask.FromResult(definition);
    }
}
