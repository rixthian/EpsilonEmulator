namespace Epsilon.CoreGame;

public interface IRoomRuntimeCoordinator
{
    ValueTask SignalMutationAsync(
        RoomId roomId,
        RoomRuntimeMutationKind mutationKind,
        CancellationToken cancellationToken = default);

    ValueTask<RoomRuntimeCoordinationSnapshot?> GetSnapshotAsync(
        RoomId roomId,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<RoomRuntimeCoordinationSnapshot>> GetSnapshotsAsync(
        CancellationToken cancellationToken = default);
}
