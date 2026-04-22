using Epsilon.CoreGame;

namespace Epsilon.Persistence;

internal sealed class InMemoryRoomBotDefinitionRepository : IRoomBotDefinitionRepository
{
    private readonly InMemoryHotelStore _store;

    public InMemoryRoomBotDefinitionRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<IReadOnlyList<HotelBotDefinition>> GetByAssetPackageKeyAsync(
        string assetPackageKey,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<HotelBotDefinition> definitions = _store.BotDefinitions
            .Where(candidate => string.Equals(candidate.AssetPackageKey, assetPackageKey, StringComparison.OrdinalIgnoreCase))
            .OrderBy(candidate => candidate.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return ValueTask.FromResult(definitions);
    }
}
