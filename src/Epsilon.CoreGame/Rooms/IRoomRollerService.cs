namespace Epsilon.CoreGame;

public interface IRoomRollerService
{
    ValueTask<int> ProcessAsync(RoomId roomId, CancellationToken cancellationToken = default);
}
