namespace Epsilon.CoreGame;

public interface ICollectorProgressRepo
{
    ValueTask<CollectorProgressSnapshot?> GetByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);

    ValueTask StoreAsync(
        CollectorProgressSnapshot snapshot,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically advances LastEmeraldAccrualUtc to <paramref name="now"/> only if
    /// the current value is null or older than <paramref name="requiredGapHours"/> hours.
    /// Returns the updated snapshot on success, or null if the gate has not elapsed.
    /// </summary>
    ValueTask<CollectorProgressSnapshot?> TryAdvanceAccrualTimestampAsync(
        CharacterId characterId,
        DateTime now,
        int requiredGapHours,
        CancellationToken cancellationToken = default);
}
