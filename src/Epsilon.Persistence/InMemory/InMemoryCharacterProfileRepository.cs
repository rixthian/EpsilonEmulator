using Epsilon.CoreGame;

namespace Epsilon.Persistence;

internal sealed class InMemoryCharacterProfileRepository : ICharacterProfileRepository
{
    private readonly InMemoryHotelStore _store;
    private readonly object _sync = new();

    public InMemoryCharacterProfileRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<CharacterProfile?> GetByIdAsync(CharacterId characterId, CancellationToken cancellationToken = default)
    {
        _store.Characters.TryGetValue(characterId, out CharacterProfile? profile);
        return ValueTask.FromResult(profile);
    }

    public ValueTask<CharacterProfile?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        CharacterProfile? profile = _store.Characters.Values.FirstOrDefault(candidate =>
            string.Equals(candidate.Username, username, StringComparison.OrdinalIgnoreCase));
        return ValueTask.FromResult(profile);
    }

    public ValueTask<CharacterProfile> CreateAsync(
        AccountId accountId,
        string username,
        RoomId homeRoomId,
        CancellationToken cancellationToken = default)
    {
        CharacterProfile profile;

        lock (_sync)
        {
            CharacterId newId = new(_store.NextCharacterId++);
            profile = new CharacterProfile(
                CharacterId: newId,
                AccountId: accountId,
                Username: username,
                Motto: "New arrival.",
                Figure: "hd-180-1.ch-210-66.lg-270-82.sh-290-80",
                Gender: "M",
                HomeRoomId: homeRoomId,
                CreditsBalance: 200,
                ActivityPointsBalance: 0,
                RespectPoints: 0,
                DailyRespectPoints: 3,
                DailyPetRespectPoints: 3);
            _store.Characters[newId] = profile;
        }

        return ValueTask.FromResult(profile);
    }
}
