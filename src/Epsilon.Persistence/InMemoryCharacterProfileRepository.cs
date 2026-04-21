using Epsilon.CoreGame;

namespace Epsilon.Persistence;

internal sealed class InMemoryCharacterProfileRepository : ICharacterProfileRepository
{
    private readonly InMemoryHotelStore _store;

    public InMemoryCharacterProfileRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<CharacterProfile?> GetByIdAsync(CharacterId characterId, CancellationToken cancellationToken = default)
    {
        _store.Characters.TryGetValue(characterId, out CharacterProfile? profile);
        return ValueTask.FromResult(profile);
    }
}

