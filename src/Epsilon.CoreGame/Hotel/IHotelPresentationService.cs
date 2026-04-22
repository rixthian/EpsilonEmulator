namespace Epsilon.CoreGame;

public interface IHotelPresentationService
{
    ValueTask<CatalogLandingSnapshot> GetCatalogLandingAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);

    ValueTask<RoomVisualSnapshot?> GetRoomVisualSnapshotAsync(
        RoomId roomId,
        CancellationToken cancellationToken = default);
}
