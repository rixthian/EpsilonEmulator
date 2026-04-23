namespace Epsilon.CoreGame;

public interface ICharacterProfileRepository
{
    ValueTask<CharacterProfile?> GetByIdAsync(CharacterId characterId, CancellationToken cancellationToken = default);
    ValueTask<CharacterProfile?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    ValueTask<CharacterProfile?> GetByPublicIdAsync(string publicId, CancellationToken cancellationToken = default);
    ValueTask<CharacterProfile> CreateAsync(AccountId accountId, string username, RoomId homeRoomId, CancellationToken cancellationToken = default);
    ValueTask StoreAsync(CharacterProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically decrements DailyRespectPoints by 1 only if the current value is > 0.
    /// Returns the updated profile on success, or null if points are exhausted.
    /// </summary>
    ValueTask<CharacterProfile?> TrySpendDailyRespectAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);
}
