using Epsilon.CoreGame;
using System.Collections.Concurrent;

namespace Epsilon.Persistence;

internal sealed class InMemoryRoomRuntimeRepository : IRoomRuntimeRepository
{
    private readonly InMemoryHotelStore _store;
    // Room-local shards keep hot rooms from serializing the whole hotel behind
    // one lock. This is the minimum viable shape for a fast single-node runtime.
    private readonly ConcurrentDictionary<RoomId, RoomRuntimeShardState> _shards = new();
    // Used only when iterating _store.RoomActors.Keys across all rooms, which
    // cannot be done safely with the per-shard locks alone.
    private readonly object _storeKeysLock = new();

    public InMemoryRoomRuntimeRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<RoomActorState?> GetActorByIdAsync(
        RoomId roomId,
        long actorId,
        CancellationToken cancellationToken = default)
    {
        RoomRuntimeShardState shard = GetShard(roomId);
        lock (shard.SyncRoot)
        {
            RoomActorState? actor = null;
            if (_store.RoomActors.TryGetValue(roomId, out List<RoomActorState>? actors))
            {
                actor = actors.FirstOrDefault(candidate =>
                    candidate.ActorKind == RoomActorKind.Player && candidate.ActorId == actorId);

                if (actor is null && actorId < 0)
                {
                    actor = actors.FirstOrDefault(candidate => candidate.ActorId == actorId);
                }
            }

            return ValueTask.FromResult(actor);
        }
    }

    public ValueTask<IReadOnlyList<RoomActorState>> GetActorsByRoomIdAsync(
        RoomId roomId,
        CancellationToken cancellationToken = default)
    {
        RoomRuntimeShardState shard = GetShard(roomId);
        lock (shard.SyncRoot)
        {
            // A snapshot copy avoids exposing the mutable backing list to callers.
            IReadOnlyList<RoomActorState> result = _store.RoomActors.TryGetValue(roomId, out List<RoomActorState>? actors)
                ? actors.ToArray()
                : [];

            return ValueTask.FromResult(result);
        }
    }

    public ValueTask<RoomActivitySnapshot?> GetActivityByRoomIdAsync(
        RoomId roomId,
        CancellationToken cancellationToken = default)
    {
        RoomRuntimeShardState shard = GetShard(roomId);
        lock (shard.SyncRoot)
        {
            _store.RoomActivities.TryGetValue(roomId, out RoomActivitySnapshot? activity);
            return ValueTask.FromResult(activity);
        }
    }

    public ValueTask<RoomChatPolicySnapshot?> GetChatPolicyByRoomIdAsync(
        RoomId roomId,
        CancellationToken cancellationToken = default)
    {
        RoomRuntimeShardState shard = GetShard(roomId);
        lock (shard.SyncRoot)
        {
            _store.RoomChatPolicies.TryGetValue(roomId, out RoomChatPolicySnapshot? policy);
            return ValueTask.FromResult(policy);
        }
    }

    public ValueTask StoreActorStateAsync(
        RoomId roomId,
        RoomActorState actorState,
        CancellationToken cancellationToken = default)
    {
        // HOTFIX deadlock: The original code nested _storeKeysLock INSIDE shard.SyncRoot,
        // while RemoveActorFromAllRoomsAsync takes the opposite order (_storeKeysLock first,
        // then shard.SyncRoot). Classic lock-order inversion → deadlock under concurrency.
        // Fix: ensure the backing list exists under _storeKeysLock BEFORE acquiring the shard lock.
        lock (_storeKeysLock)
        {
            if (!_store.RoomActors.ContainsKey(roomId))
                _store.RoomActors[roomId] = [];
        }

        RoomRuntimeShardState shard = GetShard(roomId);
        lock (shard.SyncRoot)
        {
            // List is guaranteed to exist at this point.
            if (!_store.RoomActors.TryGetValue(roomId, out List<RoomActorState>? actors))
                return ValueTask.CompletedTask;

            int index = actors.FindIndex(candidate =>
                candidate.ActorKind == actorState.ActorKind &&
                candidate.ActorId == actorState.ActorId);
            if (index >= 0)
                actors[index] = actorState;
            else
                actors.Add(actorState);

            return ValueTask.CompletedTask;
        }
    }

    public ValueTask<IReadOnlyList<RoomId>> RemoveActorFromAllRoomsAsync(
        long actorId,
        CancellationToken cancellationToken = default)
    {
        List<RoomId> removedRoomIds = [];
        // HOTFIX deadlock: Collect rooms that become empty in a side list and prune their
        // keys in a second pass AFTER all shard locks are released.  The original code
        // re-acquired _storeKeysLock while still holding shard.SyncRoot — the exact
        // opposite of the lock order used in StoreActorStateAsync → deadlock.
        List<RoomId> emptyRoomIds = [];

        RoomId[] roomIds;
        lock (_storeKeysLock)
        {
            roomIds = _store.RoomActors.Keys
                .OrderBy(static roomId => roomId.Value)
                .ToArray();
        }

        foreach (RoomId roomId in roomIds)
        {
            RoomRuntimeShardState shard = GetShard(roomId);
            lock (shard.SyncRoot)
            {
                if (!_store.RoomActors.TryGetValue(roomId, out List<RoomActorState>? actors))
                    continue;

                int removedCount = actors.RemoveAll(candidate =>
                    candidate.ActorKind == RoomActorKind.Player &&
                    candidate.ActorId == actorId);
                if (removedCount == 0)
                    continue;

                removedRoomIds.Add(roomId);
                if (actors.Count == 0)
                    emptyRoomIds.Add(roomId);
            }
        }

        // Second pass: prune empty-room keys under _storeKeysLock with no shard lock held.
        if (emptyRoomIds.Count > 0)
        {
            lock (_storeKeysLock)
            {
                foreach (RoomId emptyRoomId in emptyRoomIds)
                {
                    if (_store.RoomActors.TryGetValue(emptyRoomId, out List<RoomActorState>? remaining) &&
                        remaining.Count == 0)
                    {
                        _store.RoomActors.Remove(emptyRoomId);
                    }
                }
            }
        }

        return ValueTask.FromResult<IReadOnlyList<RoomId>>(removedRoomIds);
    }

    public ValueTask StoreChatPolicyAsync(
        RoomId roomId,
        RoomChatPolicySnapshot chatPolicy,
        CancellationToken cancellationToken = default)
    {
        RoomRuntimeShardState shard = GetShard(roomId);
        lock (shard.SyncRoot)
        {
            _store.RoomChatPolicies[roomId] = chatPolicy;
            return ValueTask.CompletedTask;
        }
    }

    public ValueTask<IReadOnlyList<RoomChatMessage>> GetChatMessagesByRoomIdAsync(
        RoomId roomId,
        CancellationToken cancellationToken = default)
    {
        RoomRuntimeShardState shard = GetShard(roomId);
        lock (shard.SyncRoot)
        {
            // Chat history is returned in message-id order so read models stay
            // stable even when messages were appended concurrently.
            IReadOnlyList<RoomChatMessage> result = _store.RoomChatMessages.TryGetValue(roomId, out List<RoomChatMessage>? messages)
                ? messages.OrderBy(message => message.MessageId).ToArray()
                : [];

            return ValueTask.FromResult(result);
        }
    }

    public ValueTask<IReadOnlyList<RoomChatMessage>> GetPrivateChatMessagesByActorAsync(
        RoomId roomId,
        long actorId,
        CancellationToken cancellationToken = default)
    {
        RoomRuntimeShardState shard = GetShard(roomId);
        lock (shard.SyncRoot)
        {
            IReadOnlyList<RoomChatMessage> result = _store.PrivateRoomChatMessages.TryGetValue(roomId, out List<RoomChatMessage>? messages)
                ? messages
                    .Where(message => message.SenderActorId == actorId || message.RecipientActorId == actorId)
                    .OrderBy(message => message.MessageId)
                    .ToArray()
                : [];

            return ValueTask.FromResult(result);
        }
    }

    public ValueTask<RoomChatMessage> AppendChatMessageAsync(
        RoomId roomId,
        long senderActorId,
        string senderName,
        string message,
        RoomChatMessageKind messageKind,
        CancellationToken cancellationToken = default)
    {
        RoomRuntimeShardState shard = GetShard(roomId);
        lock (shard.SyncRoot)
        {
            if (!_store.RoomChatMessages.TryGetValue(roomId, out List<RoomChatMessage>? messages))
            {
                messages = [];
                _store.RoomChatMessages[roomId] = messages;
            }

            RoomChatMessage chatMessage = new(
                MessageId: shard.NextMessageId++,
                RoomId: roomId,
                SenderActorId: senderActorId,
                SenderName: senderName,
                Message: message,
                MessageKind: messageKind,
                RecipientActorId: null,
                RecipientName: null,
                SentAtUtc: DateTime.UtcNow);

            messages.Add(chatMessage);

            // HOTFIX memory leak: cap chat history so rooms with heavy traffic don't grow unboundedly.
            const int MaxChatHistory = 200;
            if (messages.Count > MaxChatHistory)
                messages.RemoveRange(0, messages.Count - MaxChatHistory);

            return ValueTask.FromResult(chatMessage);
        }
    }

    public ValueTask<RoomChatMessage> AppendPrivateChatMessageAsync(
        RoomId roomId,
        long senderActorId,
        string senderName,
        long recipientActorId,
        string recipientName,
        string message,
        RoomChatMessageKind messageKind,
        CancellationToken cancellationToken = default)
    {
        RoomRuntimeShardState shard = GetShard(roomId);
        lock (shard.SyncRoot)
        {
            if (!_store.PrivateRoomChatMessages.TryGetValue(roomId, out List<RoomChatMessage>? messages))
            {
                messages = [];
                _store.PrivateRoomChatMessages[roomId] = messages;
            }

            RoomChatMessage chatMessage = new(
                MessageId: shard.NextMessageId++,
                RoomId: roomId,
                SenderActorId: senderActorId,
                SenderName: senderName,
                Message: message,
                MessageKind: messageKind,
                RecipientActorId: recipientActorId,
                RecipientName: recipientName,
                SentAtUtc: DateTime.UtcNow);

            messages.Add(chatMessage);

            // HOTFIX memory leak: cap private chat history per room for the same
            // reason as public chat — whisper-heavy rooms would otherwise grow without bound.
            const int MaxPrivateChatHistory = 500;
            if (messages.Count > MaxPrivateChatHistory)
                messages.RemoveRange(0, messages.Count - MaxPrivateChatHistory);

            return ValueTask.FromResult(chatMessage);
        }
    }

    public ValueTask<IReadOnlyList<RoomId>> GetAllActiveRoomIdsAsync(CancellationToken cancellationToken = default)
    {
        lock (_storeKeysLock)
        {
            IReadOnlyList<RoomId> roomIds = _store.RoomActors
                .Where(pair => pair.Value.Count > 0)
                .Select(pair => pair.Key)
                .ToArray();
            return ValueTask.FromResult(roomIds);
        }
    }

    public ValueTask<int> EvictAllPlayersFromRoomAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        // HOTFIX deadlock: same pattern — never acquire _storeKeysLock while shard.SyncRoot is held.
        RoomRuntimeShardState shard = GetShard(roomId);
        int removed;
        bool pruneKey;

        lock (shard.SyncRoot)
        {
            if (!_store.RoomActors.TryGetValue(roomId, out List<RoomActorState>? actors))
                return ValueTask.FromResult(0);

            removed = actors.RemoveAll(static actor => actor.ActorKind == RoomActorKind.Player);
            pruneKey = actors.Count == 0;
        }

        if (pruneKey)
        {
            lock (_storeKeysLock)
            {
                if (_store.RoomActors.TryGetValue(roomId, out List<RoomActorState>? remaining) &&
                    remaining.Count == 0)
                {
                    _store.RoomActors.Remove(roomId);
                }
            }
        }

        return ValueTask.FromResult(removed);
    }

    public ValueTask<RoomId?> FindRoomForActorAsync(long actorId, CancellationToken cancellationToken = default)
    {
        RoomId[] roomIds;
        lock (_storeKeysLock)
        {
            roomIds = _store.RoomActors.Keys
                .OrderBy(static roomId => roomId.Value)
                .ToArray();
        }

        foreach (RoomId roomId in roomIds)
        {
            RoomRuntimeShardState shard = GetShard(roomId);
            lock (shard.SyncRoot)
            {
                if (_store.RoomActors.TryGetValue(roomId, out List<RoomActorState>? actors) &&
                    actors.Any(actor =>
                        actor.ActorKind == RoomActorKind.Player &&
                        actor.ActorId == actorId))
                {
                    return ValueTask.FromResult<RoomId?>(roomId);
                }
            }
        }

        return ValueTask.FromResult<RoomId?>(null);
    }

    private RoomRuntimeShardState GetShard(RoomId roomId)
    {
        return _shards.GetOrAdd(
            roomId,
            roomKey =>
            {
                long nextMessageId = _store.RoomChatMessages.TryGetValue(roomKey, out List<RoomChatMessage>? messages)
                    ? messages.Select(message => message.MessageId).DefaultIfEmpty(0).Max() + 1
                    : 1;

                return new RoomRuntimeShardState(nextMessageId);
            });
    }
}
