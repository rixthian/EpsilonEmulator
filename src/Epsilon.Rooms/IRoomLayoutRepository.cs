namespace Epsilon.Rooms;

public interface IRoomLayoutRepository
{
    ValueTask<RoomLayoutDefinition?> GetByCodeAsync(string layoutCode, CancellationToken cancellationToken = default);
}

