using Epsilon.CoreGame;

namespace Epsilon.Persistence;

internal sealed class InMemoryCollectorProgressRepo : ICollectorProgressRepo
{
    private readonly InMemoryHotelStore _store;
    private readonly object _sync = new();

    public InMemoryCollectorProgressRepo(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<CollectorProgressSnapshot?> GetByCharacterIdAsync(CharacterId characterId, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _store.CollectorProgress.TryGetValue(characterId, out CollectorProgressSnapshot? snapshot);
            return ValueTask.FromResult(snapshot);
        }
    }

    public ValueTask StoreAsync(CollectorProgressSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _store.CollectorProgress[snapshot.CharacterId] = snapshot;
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask<CollectorProgressSnapshot?> TryAdvanceAccrualTimestampAsync(
        CharacterId characterId,
        DateTime now,
        int requiredGapHours,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _store.CollectorProgress.TryGetValue(characterId, out CollectorProgressSnapshot? current);

            if (current is not null &&
                current.LastEmeraldAccrualUtc is not null &&
                current.LastEmeraldAccrualUtc.Value > now.AddHours(-requiredGapHours))
            {
                return ValueTask.FromResult<CollectorProgressSnapshot?>(null);
            }

            CollectorProgressSnapshot updated = current is null
                ? new CollectorProgressSnapshot(characterId, 0, 1, 0, 100, "bronze", now)
                : current with { LastEmeraldAccrualUtc = now };

            _store.CollectorProgress[characterId] = updated;
            return ValueTask.FromResult<CollectorProgressSnapshot?>(updated);
        }
    }
}
