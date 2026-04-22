using Epsilon.Content;
using Epsilon.CoreGame;

namespace Epsilon.Persistence;

internal sealed class InMemoryNavigatorPublicRoomRepository : INavigatorPublicRoomRepository
{
    private readonly InMemoryHotelStore _store;

    public InMemoryNavigatorPublicRoomRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<NavigatorPublicRoomDefinition?> GetByEntryIdAsync(int entryId, CancellationToken cancellationToken = default)
    {
        _store.NavigatorPublicRooms.TryGetValue(entryId, out NavigatorPublicRoomDefinition? entry);
        return ValueTask.FromResult(entry);
    }

    public ValueTask<NavigatorPublicRoomDefinition?> GetByRoomIdAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        NavigatorPublicRoomDefinition? entry = _store.NavigatorPublicRooms.Values
            .FirstOrDefault(candidate => candidate.RoomId == roomId);

        return ValueTask.FromResult(entry);
    }

    public ValueTask<IReadOnlyList<NavigatorPublicRoomDefinition>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<NavigatorPublicRoomDefinition> entries = _store.NavigatorPublicRooms.Values
            .OrderBy(entry => entry.OrderNumber)
            .ToArray();

        return ValueTask.FromResult(entries);
    }
}
