using Epsilon.Content;
using Epsilon.Rooms;

namespace Epsilon.CoreGame;

public interface IRoomBotRuntimeService
{
    ValueTask<IReadOnlyList<RoomActorState>> EnsureRoomBotsAsync(
        RoomHotelSnapshot room,
        NavigatorPublicRoomDefinition? publicRoomEntry,
        CancellationToken cancellationToken = default);

    ValueTask<RoomChatMessage?> TryHandlePlayerChatAsync(
        RoomId roomId,
        CharacterId characterId,
        string message,
        CancellationToken cancellationToken = default);
}
