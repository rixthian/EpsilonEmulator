using Epsilon.CoreGame;

namespace Epsilon.Persistence;

internal sealed class InMemoryCharacterPreferenceRepository : ICharacterPreferenceRepository
{
    private readonly InMemoryHotelStore _store;

    public InMemoryCharacterPreferenceRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<CharacterInterfacePreference?> GetByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        _store.InterfacePreferences.TryGetValue(characterId, out CharacterInterfacePreference? preference);
        return ValueTask.FromResult(preference);
    }

    public ValueTask StoreAsync(
        CharacterInterfacePreference preference,
        CancellationToken cancellationToken = default)
    {
        _store.InterfacePreferences[preference.CharacterId] = preference;
        return ValueTask.CompletedTask;
    }
}
