using Epsilon.CoreGame;
using Epsilon.Rooms;

namespace Epsilon.Persistence;

internal sealed class InMemoryRoomItemRepository : IRoomItemRepository
{
    private readonly InMemoryHotelStore _store;
    private readonly object _syncRoot = new();

    public InMemoryRoomItemRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<IReadOnlyList<RoomItemState>> GetByRoomIdAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            IReadOnlyList<RoomItemState> result = _store.RoomItems.TryGetValue(roomId, out List<RoomItemState>? items)
                ? items.ToArray()
                : [];

            return ValueTask.FromResult(result);
        }
    }

    public ValueTask StoreByRoomIdAsync(
        RoomId roomId,
        IReadOnlyList<RoomItemState> items,
        CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            _store.RoomItems[roomId] = items.ToList();
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask<IReadOnlyList<RoomItemState>> RemoveByRoomIdAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
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
}
