namespace Epsilon.CoreGame;

public interface ICharacterChatFilterPreferenceRepository
{
    ValueTask<CharacterChatFilterPreference?> GetByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);

    ValueTask StoreAsync(
        CharacterChatFilterPreference preference,
        CancellationToken cancellationToken = default);
}
