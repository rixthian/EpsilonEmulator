namespace Epsilon.CoreGame;

public interface IRoomRuntimeRepository
{
    ValueTask<RoomActorState?> GetActorByIdAsync(
        RoomId roomId,
        long actorId,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<RoomActorState>> GetActorsByRoomIdAsync(
        RoomId roomId,
        CancellationToken cancellationToken = default);

    ValueTask<RoomActivitySnapshot?> GetActivityByRoomIdAsync(
        RoomId roomId,
        CancellationToken cancellationToken = default);

    ValueTask<RoomChatPolicySnapshot?> GetChatPolicyByRoomIdAsync(
        RoomId roomId,
        CancellationToken cancellationToken = default);

    ValueTask StoreActorStateAsync(
        RoomId roomId,
        RoomActorState actorState,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<RoomId>> RemoveActorFromAllRoomsAsync(
        long actorId,
        CancellationToken cancellationToken = default);

    ValueTask StoreChatPolicyAsync(
        RoomId roomId,
        RoomChatPolicySnapshot chatPolicy,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<RoomChatMessage>> GetChatMessagesByRoomIdAsync(
        RoomId roomId,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<RoomChatMessage>> GetPrivateChatMessagesByActorAsync(
        RoomId roomId,
        long actorId,
        CancellationToken cancellationToken = default);

    ValueTask<RoomChatMessage> AppendChatMessageAsync(
        RoomId roomId,
        long senderActorId,
        string senderName,
        string message,
        RoomChatMessageKind messageKind,
        CancellationToken cancellationToken = default);

    ValueTask<RoomChatMessage> AppendPrivateChatMessageAsync(
        RoomId roomId,
        long senderActorId,
        string senderName,
        long recipientActorId,
        string recipientName,
        string message,
        RoomChatMessageKind messageKind,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the ids of all rooms that currently have at least one actor present.
    /// Used for hotel-wide broadcasts and emergency evictions.
    /// </summary>
    ValueTask<IReadOnlyList<RoomId>> GetAllActiveRoomIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes every player actor from the specified room.
    /// Non-player actors (pets, NPCs) are left in place.
    /// </summary>
    ValueTask<int> EvictAllPlayersFromRoomAsync(RoomId roomId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the room that currently contains the given actor, or null if the
    /// actor is not present in any active room. Scans all rooms in a single pass
    /// rather than issuing one lookup per room.
    /// </summary>
    ValueTask<RoomId?> FindRoomForActorAsync(long actorId, CancellationToken cancellationToken = default);
}
