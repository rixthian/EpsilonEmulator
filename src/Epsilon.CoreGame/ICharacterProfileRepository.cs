namespace Epsilon.CoreGame;

public interface ICharacterProfileRepository
{
    ValueTask<CharacterProfile?> GetByIdAsync(CharacterId characterId, CancellationToken cancellationToken = default);
}

