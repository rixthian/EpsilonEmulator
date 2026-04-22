using Epsilon.CoreGame;
using Epsilon.Rooms;

namespace Epsilon.Persistence;

internal sealed class InMemoryRoomItemRepository : IRoomItemRepository
{
    private readonly InMemoryHotelStore _store;

    public InMemoryRoomItemRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<IReadOnlyList<RoomItemState>> GetByRoomIdAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<RoomItemState> result = _store.RoomItems.TryGetValue(roomId, out List<RoomItemState>? items)
            ? items
            : [];

        return ValueTask.FromResult(result);
    }

    public ValueTask<IReadOnlyList<RoomItemState>> RemoveByRoomIdAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        if (!_store.RoomItems.TryGetValue(roomId, out List<RoomItemState>? items))
        {
            return ValueTask.FromResult<IReadOnlyList<RoomItemState>>([]);
        }

        RoomItemState[] snapshot = items.ToArray();
        _store.RoomItems.Remove(roomId);
        return ValueTask.FromResult<IReadOnlyList<RoomItemState>>(snapshot);
    }
}
