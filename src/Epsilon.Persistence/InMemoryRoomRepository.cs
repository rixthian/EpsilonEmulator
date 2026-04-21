using Epsilon.CoreGame;
using Epsilon.Rooms;

namespace Epsilon.Persistence;

internal sealed class InMemoryRoomRepository : IRoomRepository
{
    private readonly InMemoryHotelStore _store;

    public InMemoryRoomRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<RoomDefinition?> GetByIdAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        _store.Rooms.TryGetValue(roomId, out RoomDefinition? room);
        return ValueTask.FromResult(room);
    }
}

