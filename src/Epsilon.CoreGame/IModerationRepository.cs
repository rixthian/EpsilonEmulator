namespace Epsilon.CoreGame;

public interface IModerationRepository
{
    ValueTask<ModerationBanRecord?> GetActiveBanByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);

    ValueTask StoreBanAsync(
        ModerationBanRecord banRecord,
        CancellationToken cancellationToken = default);
}
