using Epsilon.Rooms;

namespace Epsilon.Persistence;

internal sealed class InMemoryRoomVisualSceneRepository : IRoomVisualSceneRepository
{
    private readonly InMemoryHotelStore _store;

    public InMemoryRoomVisualSceneRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<RoomVisualSceneDefinition?> GetByLayoutCodeAsync(
        string layoutCode,
        CancellationToken cancellationToken = default)
    {
        RoomVisualSceneDefinition? scene = _store.RoomVisualScenes
            .FirstOrDefault(candidate => string.Equals(candidate.LayoutCode, layoutCode, StringComparison.OrdinalIgnoreCase));

        return ValueTask.FromResult(scene);
    }
}
