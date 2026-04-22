namespace Epsilon.CoreGame;

public interface ISubscriptionRepository
{
    ValueTask<IReadOnlyList<SubscriptionRecord>> GetByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);
}

