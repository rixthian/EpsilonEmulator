using System.Collections.Concurrent;
using Epsilon.CoreGame;

namespace Epsilon.Persistence;

internal sealed class LocalRoomRuntimeCoordinator : IRoomRuntimeCoordinator
{
    private readonly IRoomRuntimeRepository _roomRuntimeRepository;
    private readonly ConcurrentDictionary<RoomId, RoomRuntimeCoordinationSnapshot> _signals = new();

    public LocalRoomRuntimeCoordinator(IRoomRuntimeRepository roomRuntimeRepository)
    {
        _roomRuntimeRepository = roomRuntimeRepository;
    }

    public async ValueTask SignalMutationAsync(
        RoomId roomId,
        RoomRuntimeMutationKind mutationKind,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<RoomActorState> actors =
            await _roomRuntimeRepository.GetActorsByRoomIdAsync(roomId, cancellationToken);
        int actorCount = actors.Count;
        int playerCount = actors.Count(static actor => actor.ActorKind == RoomActorKind.Player);
        DateTime utcNow = DateTime.UtcNow;
        _signals.AddOrUpdate(
            roomId,
            key => new RoomRuntimeCoordinationSnapshot(
                key,
                1,
                mutationKind,
                utcNow,
                actorCount,
                playerCount),
            (key, current) => current with
            {
                RoomId = key,
                Version = current.Version + 1,
                LastMutationKind = mutationKind,
                LastMutationAtUtc = utcNow,
                ActorCount = actorCount,
                PlayerCount = playerCount
            });
    }

    public ValueTask<RoomRuntimeCoordinationSnapshot?> GetSnapshotAsync(
        RoomId roomId,
        CancellationToken cancellationToken = default)
    {
        _signals.TryGetValue(roomId, out RoomRuntimeCoordinationSnapshot? snapshot);
        return ValueTask.FromResult(snapshot);
    }

    public ValueTask<IReadOnlyList<RoomRuntimeCoordinationSnapshot>> GetSnapshotsAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<RoomRuntimeCoordinationSnapshot> snapshots = _signals.Values
            .OrderByDescending(static snapshot => snapshot.LastMutationAtUtc)
            .ToArray();

        return ValueTask.FromResult(snapshots);
    }
}
