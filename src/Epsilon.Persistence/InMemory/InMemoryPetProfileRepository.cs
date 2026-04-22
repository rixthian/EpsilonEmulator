using Epsilon.CoreGame;

namespace Epsilon.Persistence;

internal sealed class InMemoryPetProfileRepository : IPetProfileRepository
{
    private readonly InMemoryHotelStore _store;

    public InMemoryPetProfileRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<IReadOnlyList<PetProfile>> GetByCharacterIdAsync(CharacterId characterId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PetProfile> result = _store.Pets.TryGetValue(characterId, out List<PetProfile>? pets)
            ? pets
            : [];

        return ValueTask.FromResult(result);
    }
}

