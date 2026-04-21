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

    ValueTask StoreChatPolicyAsync(
        RoomId roomId,
        RoomChatPolicySnapshot chatPolicy,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<RoomChatMessage>> GetChatMessagesByRoomIdAsync(
        RoomId roomId,
        CancellationToken cancellationToken = default);

    ValueTask<RoomChatMessage> AppendChatMessageAsync(
        RoomId roomId,
        long senderActorId,
        string senderName,
        string message,
        RoomChatMessageKind messageKind,
        CancellationToken cancellationToken = default);
}
