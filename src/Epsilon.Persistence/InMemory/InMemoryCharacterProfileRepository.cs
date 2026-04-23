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
        lock (_sync)
        {
            _store.Characters.TryGetValue(characterId, out CharacterProfile? profile);
            return ValueTask.FromResult(profile);
        }
    }

    public ValueTask<CharacterProfile?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            CharacterProfile? profile = _store.Characters.Values.FirstOrDefault(candidate =>
                string.Equals(candidate.Username, username, StringComparison.OrdinalIgnoreCase));
            return ValueTask.FromResult(profile);
        }
    }

    public ValueTask<CharacterProfile?> GetByPublicIdAsync(string publicId, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            CharacterProfile? profile = _store.Characters.Values.FirstOrDefault(candidate =>
                string.Equals(candidate.PublicId, publicId, StringComparison.OrdinalIgnoreCase));
            return ValueTask.FromResult(profile);
        }
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
            if (_store.Characters.Values.Any(candidate =>
                    string.Equals(candidate.Username, username, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("username_taken");
            }

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
                DailyPetRespectPoints: 3,
                PublicId: BuildPublicId(newId));
            _store.Characters[newId] = profile;
        }

        return ValueTask.FromResult(profile);
    }

    public ValueTask StoreAsync(CharacterProfile profile, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _store.Characters[profile.CharacterId] = profile;
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask<CharacterProfile?> TrySpendDailyRespectAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (!_store.Characters.TryGetValue(characterId, out CharacterProfile? profile) ||
                profile.DailyRespectPoints <= 0)
            {
                return ValueTask.FromResult<CharacterProfile?>(null);
            }

            CharacterProfile updated = profile with { DailyRespectPoints = profile.DailyRespectPoints - 1 };
            _store.Characters[characterId] = updated;
            return ValueTask.FromResult<CharacterProfile?>(updated);
        }
    }

    private static string BuildPublicId(CharacterId characterId)
    {
        return $"usr_{characterId.Value:x8}";
    }
}
