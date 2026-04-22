namespace Epsilon.CoreGame;

public interface IPetProfileRepository
{
    ValueTask<IReadOnlyList<PetProfile>> GetByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);
}

