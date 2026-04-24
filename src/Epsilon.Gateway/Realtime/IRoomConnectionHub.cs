using Epsilon.CoreGame;

namespace Epsilon.Gateway;

// HOTFIX broadcast: Registry of active WebSocket connections keyed by room.
// HotelRealtimeSocketHandler registers each connection when a player enters a room
// and uses BroadcastToRoomAsync to push room state snapshots to all peers whenever
// any mutation occurs (movement, chat, entry).  Without this, players never saw
// what other players were doing — the server was completely deaf in real time.
public interface IRoomConnectionHub
{
    /// <summary>
    /// Registers a send delegate for the given room and returns a unique connection id.
    /// Call Unregister with the same roomId + connectionId when leaving.
    /// </summary>
    Guid Register(RoomId roomId, Func<string, CancellationToken, Task> sendFn);

    /// <summary>
    /// Removes a specific connection from a room's broadcast list.
    /// </summary>
    void Unregister(RoomId roomId, Guid connectionId);

    /// <summary>
    /// Sends <paramref name="json"/> to every connection registered for the room,
    /// excluding the connection identified by <paramref name="excludeConnectionId"/>.
    /// Failures on individual connections are swallowed so one bad socket never
    /// blocks delivery to the rest of the room.
    /// </summary>
    Task BroadcastToRoomAsync(
        RoomId roomId,
        string json,
        Guid excludeConnectionId,
        CancellationToken cancellationToken);
}
