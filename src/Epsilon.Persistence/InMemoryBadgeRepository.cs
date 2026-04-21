using Epsilon.CoreGame;

namespace Epsilon.Persistence;

internal sealed class InMemoryBadgeRepository : IBadgeRepository
{
    private readonly InMemoryHotelStore _store;

    public InMemoryBadgeRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<IReadOnlyList<BadgeAssignment>> GetByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<BadgeAssignment> result = _store.BadgeAssignments.TryGetValue(characterId, out List<BadgeAssignment>? badges)
            ? badges
            : [];

        return ValueTask.FromResult(result);
    }
}
