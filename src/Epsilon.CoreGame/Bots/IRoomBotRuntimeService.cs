using Epsilon.Content;
using Epsilon.Rooms;

namespace Epsilon.CoreGame;

public interface IRoomBotRuntimeService
{
    ValueTask<IReadOnlyList<RoomActorState>> EnsurePublicRoomBotsAsync(
        NavigatorPublicRoomDefinition publicRoomEntry,
        RoomLayoutDefinition roomLayout,
        CancellationToken cancellationToken = default);

    ValueTask<RoomChatMessage?> TryHandlePlayerChatAsync(
        RoomId roomId,
        CharacterId characterId,
        string message,
        CancellationToken cancellationToken = default);
}
