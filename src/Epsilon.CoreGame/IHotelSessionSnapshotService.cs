namespace Epsilon.CoreGame;

public interface IHotelSessionSnapshotService
{
    ValueTask<HotelSessionSnapshot?> BuildAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);
}
