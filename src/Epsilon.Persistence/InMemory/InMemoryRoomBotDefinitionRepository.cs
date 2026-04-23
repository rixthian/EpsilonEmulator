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
            .Where(candidate =>
                candidate.RoomId is null &&
                string.Equals(candidate.AssetPackageKey, assetPackageKey, StringComparison.OrdinalIgnoreCase))
            .OrderBy(candidate => candidate.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return ValueTask.FromResult(definitions);
    }

    public ValueTask<IReadOnlyList<HotelBotDefinition>> GetByRoomIdAsync(
        RoomId roomId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<HotelBotDefinition> definitions = _store.BotDefinitions
            .Where(candidate => candidate.RoomId == roomId)
            .OrderBy(candidate => candidate.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return ValueTask.FromResult(definitions);
    }

    public ValueTask StoreAsync(
        HotelBotDefinition definition,
        CancellationToken cancellationToken = default)
    {
        int existingIndex = _store.BotDefinitions.FindIndex(candidate =>
            string.Equals(candidate.BotKey, definition.BotKey, StringComparison.OrdinalIgnoreCase));

        if (existingIndex >= 0)
        {
            _store.BotDefinitions[existingIndex] = definition;
        }
        else
        {
            _store.BotDefinitions.Add(definition);
        }

        return ValueTask.CompletedTask;
    }
}
