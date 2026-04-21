namespace Epsilon.CoreGame;

public interface IBadgeRepository
{
    ValueTask<IReadOnlyList<BadgeAssignment>> GetByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);
}
