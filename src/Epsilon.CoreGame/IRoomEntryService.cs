namespace Epsilon.CoreGame;

public interface IRoomEntryService
{
    ValueTask<RoomEntryResult> EnterAsync(
        RoomEntryRequest request,
        CancellationToken cancellationToken = default);
}
