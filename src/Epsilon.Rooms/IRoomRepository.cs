using Epsilon.CoreGame;

namespace Epsilon.Rooms;

public interface IRoomRepository
{
    ValueTask<RoomDefinition?> GetByIdAsync(RoomId roomId, CancellationToken cancellationToken = default);
}

