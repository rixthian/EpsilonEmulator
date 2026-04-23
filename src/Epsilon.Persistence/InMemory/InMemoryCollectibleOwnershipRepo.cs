using Epsilon.CoreGame;

namespace Epsilon.Persistence;

internal sealed class InMemoryCollectibleOwnershipRepo : ICollectibleOwnershipRepository
{
    private readonly InMemoryHotelStore _store;
    private readonly object _sync = new();

    public InMemoryCollectibleOwnershipRepo(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<CollectibleOwnershipSnapshot?> GetByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _store.CollectibleOwnerships.TryGetValue(characterId, out CollectibleOwnershipSnapshot? snapshot);
            return ValueTask.FromResult(snapshot);
        }
    }

    public ValueTask StoreAsync(
        CollectibleOwnershipSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _store.CollectibleOwnerships[snapshot.CharacterId] = snapshot;
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask<CollectibleOwnershipSnapshot> AddKeysAsync(
        CharacterId characterId,
        IReadOnlyList<string> keysToAdd,
        IReadOnlyDictionary<string, string> collectibleKeyToCategoryKey,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _store.CollectibleOwnerships.TryGetValue(characterId, out CollectibleOwnershipSnapshot? current);
            current ??= new CollectibleOwnershipSnapshot(characterId, null, [], [], "runtime_default", DateTime.UtcNow);

            List<string> updatedKeys = current.OwnedCollectibleKeys.ToList();
            updatedKeys.AddRange(keysToAdd);

            HashSet<string> categories = new(current.OwnedCategoryKeys, StringComparer.OrdinalIgnoreCase);
            foreach (string key in keysToAdd)
            {
                if (collectibleKeyToCategoryKey.TryGetValue(key, out string? category) &&
                    !string.IsNullOrWhiteSpace(category))
                {
                    categories.Add(category);
                }
            }

            CollectibleOwnershipSnapshot updated = current with
            {
                OwnedCollectibleKeys = updatedKeys,
                OwnedCategoryKeys = categories.OrderBy(v => v, StringComparer.OrdinalIgnoreCase).ToArray(),
                ObservedAtUtc = DateTime.UtcNow
            };

            _store.CollectibleOwnerships[characterId] = updated;
            return ValueTask.FromResult(updated);
        }
    }

    public ValueTask<(CollectibleOwnershipSnapshot Snapshot, bool Success)> TryRemoveKeysAsync(
        CharacterId characterId,
        IReadOnlyList<string> keysToRemove,
        IReadOnlyDictionary<string, string> collectibleKeyToCategoryKey,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _store.CollectibleOwnerships.TryGetValue(characterId, out CollectibleOwnershipSnapshot? current);
            current ??= new CollectibleOwnershipSnapshot(characterId, null, [], [], "runtime_default", DateTime.UtcNow);

            List<string> updatedKeys = current.OwnedCollectibleKeys.ToList();
            foreach (string key in keysToRemove)
            {
                int index = updatedKeys.FindIndex(candidate =>
                    string.Equals(candidate, key, StringComparison.OrdinalIgnoreCase));
                if (index < 0)
                {
                    return ValueTask.FromResult((current, false));
                }

                updatedKeys.RemoveAt(index);
            }

            HashSet<string> categories = new(StringComparer.OrdinalIgnoreCase);
            foreach (string key in updatedKeys)
            {
                if (collectibleKeyToCategoryKey.TryGetValue(key, out string? category) &&
                    !string.IsNullOrWhiteSpace(category))
                {
                    categories.Add(category);
                }
            }

            CollectibleOwnershipSnapshot updated = current with
            {
                OwnedCollectibleKeys = updatedKeys,
                OwnedCategoryKeys = categories.OrderBy(v => v, StringComparer.OrdinalIgnoreCase).ToArray(),
                ObservedAtUtc = DateTime.UtcNow
            };

            _store.CollectibleOwnerships[characterId] = updated;
            return ValueTask.FromResult((updated, true));
        }
    }

    public ValueTask<int> CountWithOwnedCollectiblesAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            int count = _store.CollectibleOwnerships.Values
                .Count(snapshot => snapshot.OwnedCollectibleKeys.Count > 0);
            return ValueTask.FromResult(count);
        }
    }
}
