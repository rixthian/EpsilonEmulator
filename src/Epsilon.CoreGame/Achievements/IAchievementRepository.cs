namespace Epsilon.CoreGame;

public interface IAchievementRepository
{
    ValueTask<IReadOnlyList<AchievementProgress>> GetByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);
}
