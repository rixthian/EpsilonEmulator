using Epsilon.Rooms;

namespace Epsilon.Persistence;

internal sealed class InMemoryRoomLayoutRepository : IRoomLayoutRepository
{
    private readonly InMemoryHotelStore _store;

    public InMemoryRoomLayoutRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<RoomLayoutDefinition?> GetByCodeAsync(string layoutCode, CancellationToken cancellationToken = default)
    {
        _store.Layouts.TryGetValue(layoutCode, out RoomLayoutDefinition? layout);
        return ValueTask.FromResult(layout);
    }
}

