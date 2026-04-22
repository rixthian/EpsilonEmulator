namespace Epsilon.CoreGame;

public interface ICharacterProfileRepository
{
    ValueTask<CharacterProfile?> GetByIdAsync(CharacterId characterId, CancellationToken cancellationToken = default);
    ValueTask<CharacterProfile?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    ValueTask<CharacterProfile> CreateAsync(AccountId accountId, string username, RoomId homeRoomId, CancellationToken cancellationToken = default);
}
