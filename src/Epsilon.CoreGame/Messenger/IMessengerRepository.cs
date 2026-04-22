namespace Epsilon.CoreGame;

public interface IMessengerRepository
{
    ValueTask<IReadOnlyList<MessengerContact>> GetContactsByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<MessengerRequest>> GetPendingRequestsByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);
}
