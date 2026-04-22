namespace Epsilon.Rooms;

public interface IRoomVisualSceneRepository
{
    ValueTask<RoomVisualSceneDefinition?> GetByLayoutCodeAsync(
        string layoutCode,
        CancellationToken cancellationToken = default);
}
