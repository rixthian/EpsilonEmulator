namespace Epsilon.CoreGame;

public interface IRoomBotDefinitionRepository
{
    ValueTask<IReadOnlyList<HotelBotDefinition>> GetByAssetPackageKeyAsync(
        string assetPackageKey,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<HotelBotDefinition>> GetByRoomIdAsync(
        RoomId roomId,
        CancellationToken cancellationToken = default);

    ValueTask StoreAsync(
        HotelBotDefinition definition,
        CancellationToken cancellationToken = default);
}
