namespace Epsilon.CoreGame;

public interface ICollectibleOwnershipRepository
{
    ValueTask<CollectibleOwnershipSnapshot?> GetByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);

    ValueTask StoreAsync(
        CollectibleOwnershipSnapshot snapshot,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically appends <paramref name="keysToAdd"/> to the character's owned
    /// collectibles and updates category membership using the provided lookup.
    /// </summary>
    ValueTask<CollectibleOwnershipSnapshot> AddKeysAsync(
        CharacterId characterId,
        IReadOnlyList<string> keysToAdd,
        IReadOnlyDictionary<string, string> collectibleKeyToCategoryKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically removes <paramref name="keysToRemove"/> from the character's
    /// owned collectibles. Returns (updated snapshot, true) on success, or
    /// (current snapshot, false) if any key was not found.
    /// </summary>
    ValueTask<(CollectibleOwnershipSnapshot Snapshot, bool Success)> TryRemoveKeysAsync(
        CharacterId characterId,
        IReadOnlyList<string> keysToRemove,
        IReadOnlyDictionary<string, string> collectibleKeyToCategoryKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the number of characters that own at least one collectible.
    /// </summary>
    ValueTask<int> CountWithOwnedCollectiblesAsync(CancellationToken cancellationToken = default);
}
