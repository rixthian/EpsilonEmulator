using Epsilon.Rooms;

namespace Epsilon.Persistence;

internal sealed class InMemoryPublicRoomBehaviorRepository : IPublicRoomBehaviorRepository
{
    private readonly InMemoryHotelStore _store;

    public InMemoryPublicRoomBehaviorRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<IReadOnlyList<PublicRoomBehaviorDefinition>> GetByAssetPackageKeyAsync(
        string assetPackageKey,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PublicRoomBehaviorDefinition> behaviors = _store.PublicRoomBehaviors
            .Where(candidate => string.Equals(candidate.AssetPackageKey, assetPackageKey, StringComparison.OrdinalIgnoreCase))
            .OrderBy(candidate => candidate.DisplayName)
            .ToArray();

        return ValueTask.FromResult(behaviors);
    }
}
