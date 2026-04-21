using Epsilon.CoreGame;

namespace Epsilon.Persistence;

internal sealed class InMemoryRoomRuntimeRepository : IRoomRuntimeRepository
{
    private readonly InMemoryHotelStore _store;
    private long _nextMessageId;

    public InMemoryRoomRuntimeRepository(InMemoryHotelStore store)
    {
        _store = store;
        _nextMessageId = _store.RoomChatMessages.Values
            .SelectMany(messages => messages)
            .Select(message => message.MessageId)
            .DefaultIfEmpty(0)
            .Max() + 1;
    }

    public ValueTask<RoomActorState?> GetActorByIdAsync(
        RoomId roomId,
        long actorId,
        CancellationToken cancellationToken = default)
    {
        RoomActorState? actor = null;
        if (_store.RoomActors.TryGetValue(roomId, out List<RoomActorState>? actors))
        {
            actor = actors.FirstOrDefault(candidate => candidate.ActorId == actorId);
        }

        return ValueTask.FromResult(actor);
    }

    public ValueTask<IReadOnlyList<RoomActorState>> GetActorsByRoomIdAsync(
        RoomId roomId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<RoomActorState> result = _store.RoomActors.TryGetValue(roomId, out List<RoomActorState>? actors)
            ? actors
            : [];

        return ValueTask.FromResult(result);
    }

    public ValueTask<RoomActivitySnapshot?> GetActivityByRoomIdAsync(
        RoomId roomId,
        CancellationToken cancellationToken = default)
    {
        _store.RoomActivities.TryGetValue(roomId, out RoomActivitySnapshot? activity);
        return ValueTask.FromResult(activity);
    }

    public ValueTask<RoomChatPolicySnapshot?> GetChatPolicyByRoomIdAsync(
        RoomId roomId,
        CancellationToken cancellationToken = default)
    {
        _store.RoomChatPolicies.TryGetValue(roomId, out RoomChatPolicySnapshot? policy);
        return ValueTask.FromResult(policy);
    }

    public ValueTask StoreActorStateAsync(
        RoomId roomId,
        RoomActorState actorState,
        CancellationToken cancellationToken = default)
    {
        if (!_store.RoomActors.TryGetValue(roomId, out List<RoomActorState>? actors))
        {
            actors = [];
            _store.RoomActors[roomId] = actors;
        }

        int index = actors.FindIndex(candidate => candidate.ActorId == actorState.ActorId);
        if (index >= 0)
        {
            actors[index] = actorState;
        }
        else
        {
            actors.Add(actorState);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask StoreChatPolicyAsync(
        RoomId roomId,
        RoomChatPolicySnapshot chatPolicy,
        CancellationToken cancellationToken = default)
    {
        _store.RoomChatPolicies[roomId] = chatPolicy;
        return ValueTask.CompletedTask;
    }

    public ValueTask<IReadOnlyList<RoomChatMessage>> GetChatMessagesByRoomIdAsync(
        RoomId roomId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<RoomChatMessage> result = _store.RoomChatMessages.TryGetValue(roomId, out List<RoomChatMessage>? messages)
            ? messages.OrderBy(message => message.MessageId).ToArray()
            : [];

        return ValueTask.FromResult(result);
    }

    public ValueTask<RoomChatMessage> AppendChatMessageAsync(
        RoomId roomId,
        long senderActorId,
        string senderName,
        string message,
        RoomChatMessageKind messageKind,
        CancellationToken cancellationToken = default)
    {
        if (!_store.RoomChatMessages.TryGetValue(roomId, out List<RoomChatMessage>? messages))
        {
            messages = [];
            _store.RoomChatMessages[roomId] = messages;
        }

        RoomChatMessage chatMessage = new(
            MessageId: _nextMessageId++,
            RoomId: roomId,
            SenderActorId: senderActorId,
            SenderName: senderName,
            Message: message,
            MessageKind: messageKind,
            SentAtUtc: DateTime.UtcNow);

        messages.Add(chatMessage);
        return ValueTask.FromResult(chatMessage);
    }
}
