using Epsilon.CoreGame;

namespace Epsilon.Persistence;

internal sealed class InMemoryModerationRepository : IModerationRepository
{
    private readonly InMemoryHotelStore _store;
    // Explicit lock required because InMemoryHotelStore uses plain Dictionary<>
    // which is not thread-safe. Multiple concurrent requests would otherwise
    // race on the check-then-remove pattern below.
    private readonly object _sync = new();

    public InMemoryModerationRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<ModerationBanRecord?> GetActiveBanByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (!_store.ModerationBans.TryGetValue(characterId, out ModerationBanRecord? record))
            {
                return ValueTask.FromResult<ModerationBanRecord?>(null);
            }

            if (record.IsPermanent)
            {
                return ValueTask.FromResult<ModerationBanRecord?>(record);
            }

            if (record.ExpiresAtUtc is DateTime expiresAtUtc && expiresAtUtc > DateTime.UtcNow)
            {
                return ValueTask.FromResult<ModerationBanRecord?>(record);
            }

            _store.ModerationBans.Remove(characterId);
            return ValueTask.FromResult<ModerationBanRecord?>(null);
        }
    }

    public ValueTask StoreBanAsync(
        ModerationBanRecord banRecord,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _store.ModerationBans[banRecord.TargetCharacterId] = banRecord;
        }

        return ValueTask.CompletedTask;
    }
}
