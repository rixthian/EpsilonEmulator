namespace Epsilon.CoreGame;

public interface IRoomRuntimeSnapshotService
{
    ValueTask<RoomRuntimeSnapshot?> BuildAsync(
        RoomId roomId,
        CancellationToken cancellationToken = default);
}
