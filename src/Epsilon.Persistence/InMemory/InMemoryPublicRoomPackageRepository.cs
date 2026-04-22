using Epsilon.Content;

namespace Epsilon.Persistence;

internal sealed class InMemoryPublicRoomPackageRepository : IPublicRoomPackageRepository
{
    private readonly InMemoryHotelStore _store;

    public InMemoryPublicRoomPackageRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<PublicRoomPackageDefinition?> GetByKeyAsync(
        string assetPackageKey,
        CancellationToken cancellationToken = default)
    {
        _store.PublicRoomPackages.TryGetValue(assetPackageKey, out PublicRoomPackageDefinition? definition);
        return ValueTask.FromResult(definition);
    }
}
