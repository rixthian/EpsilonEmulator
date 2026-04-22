using Epsilon.Rooms;

namespace Epsilon.CoreGame;

public interface IHotelReadService
{
    ValueTask<CharacterHotelSnapshot?> GetCharacterSnapshotAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);

    ValueTask<RoomHotelSnapshot?> GetRoomSnapshotAsync(
        RoomId roomId,
        CancellationToken cancellationToken = default);

    ValueTask<PublicRoomHotelSnapshot?> GetPublicRoomSnapshotAsync(
        int entryId,
        CancellationToken cancellationToken = default);
}
