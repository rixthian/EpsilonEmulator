namespace Epsilon.CoreGame;

public interface ICollectorProfileService
{
    ValueTask<CollectorProfileSnapshot> BuildAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);
}
