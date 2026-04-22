namespace Epsilon.CoreGame;

public interface IHotelWorldFeatureService
{
    ValueTask<GameCatalogSnapshot> GetGameCatalogAsync(
        CancellationToken cancellationToken = default);

    ValueTask<PublicRoomBehaviorSnapshot?> GetPublicRoomBehaviorSnapshotAsync(
        int entryId,
        CancellationToken cancellationToken = default);
}
