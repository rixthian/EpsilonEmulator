namespace Epsilon.CoreGame;

public interface ICharacterPreferenceRepository
{
    ValueTask<CharacterInterfacePreference?> GetByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);

    ValueTask StoreAsync(
        CharacterInterfacePreference preference,
        CancellationToken cancellationToken = default);
}
