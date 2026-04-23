using Epsilon.CoreGame;

namespace Epsilon.Persistence;

internal sealed class InMemoryCharacterChatFilterPreferenceRepository : ICharacterChatFilterPreferenceRepository
{
    private readonly InMemoryHotelStore _store;

    public InMemoryCharacterChatFilterPreferenceRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<CharacterChatFilterPreference?> GetByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        _store.ChatFilterPreferences.TryGetValue(characterId, out CharacterChatFilterPreference? preference);
        return ValueTask.FromResult(preference);
    }

    public ValueTask StoreAsync(
        CharacterChatFilterPreference preference,
        CancellationToken cancellationToken = default)
    {
        _store.ChatFilterPreferences[preference.CharacterId] = preference;
        return ValueTask.CompletedTask;
    }
}
