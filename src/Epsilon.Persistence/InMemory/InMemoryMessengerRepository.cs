using Epsilon.CoreGame;

namespace Epsilon.Persistence;

internal sealed class InMemoryMessengerRepository : IMessengerRepository
{
    private readonly InMemoryHotelStore _store;

    public InMemoryMessengerRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<IReadOnlyList<MessengerContact>> GetContactsByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<MessengerContact> result = _store.MessengerContacts.TryGetValue(characterId, out List<MessengerContact>? contacts)
            ? contacts
            : [];

        return ValueTask.FromResult(result);
    }

    public ValueTask<IReadOnlyList<MessengerRequest>> GetPendingRequestsByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<MessengerRequest> result =
            _store.PendingMessengerRequests.TryGetValue(characterId, out List<MessengerRequest>? requests)
                ? requests
                : [];

        return ValueTask.FromResult(result);
    }
}
