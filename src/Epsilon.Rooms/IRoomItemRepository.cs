using Epsilon.CoreGame;

namespace Epsilon.Rooms;

public interface IRoomItemRepository
{
    ValueTask<IReadOnlyList<RoomItemState>> GetByRoomIdAsync(RoomId roomId, CancellationToken cancellationToken = default);
}

