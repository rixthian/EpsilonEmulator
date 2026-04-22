namespace Epsilon.CoreGame;

public interface IHousekeepingSnapshotService
{
    ValueTask<HousekeepingSnapshot?> BuildAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);
}
