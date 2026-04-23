namespace Epsilon.CoreGame;

public interface IRoomAnimService
{
    ValueTask<RoomAnimSnapshot?> BuildAsync(
        RoomId roomId,
        CancellationToken cancellationToken = default);
}
