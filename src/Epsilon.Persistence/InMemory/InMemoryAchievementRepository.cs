using Epsilon.CoreGame;

namespace Epsilon.Persistence;

internal sealed class InMemoryAchievementRepository : IAchievementRepository
{
    private readonly InMemoryHotelStore _store;

    public InMemoryAchievementRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<IReadOnlyList<AchievementProgress>> GetByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AchievementProgress> result =
            _store.AchievementProgress.TryGetValue(characterId, out List<AchievementProgress>? achievements)
                ? achievements
                : [];

        return ValueTask.FromResult(result);
    }
}
