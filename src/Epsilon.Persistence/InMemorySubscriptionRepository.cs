using Epsilon.CoreGame;

namespace Epsilon.Persistence;

internal sealed class InMemorySubscriptionRepository : ISubscriptionRepository
{
    private readonly InMemoryHotelStore _store;

    public InMemorySubscriptionRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<IReadOnlyList<SubscriptionRecord>> GetByCharacterIdAsync(CharacterId characterId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<SubscriptionRecord> result = _store.Subscriptions.TryGetValue(characterId, out List<SubscriptionRecord>? subscriptions)
            ? subscriptions
            : [];

        return ValueTask.FromResult(result);
    }
}

