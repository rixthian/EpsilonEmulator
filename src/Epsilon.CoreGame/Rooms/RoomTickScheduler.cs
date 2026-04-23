namespace Epsilon.CoreGame;

public sealed class RoomTickScheduler : IRoomTickScheduler
{
    private readonly IRoomRuntimeRepository _roomRuntimeRepository;
    private readonly IRoomRollerService _roomRollerService;
    private long _tickCounter;

    public RoomTickScheduler(
        IRoomRuntimeRepository roomRuntimeRepository,
        IRoomRollerService roomRollerService)
    {
        _roomRuntimeRepository = roomRuntimeRepository;
        _roomRollerService = roomRollerService;
    }

    public async ValueTask<int> TickAsync(int rollerIntervalTicks, CancellationToken cancellationToken = default)
    {
        if (rollerIntervalTicks <= 0)
        {
            return 0;
        }

        long tick = Interlocked.Increment(ref _tickCounter);
        if (tick % rollerIntervalTicks != 0)
        {
            return 0;
        }

        IReadOnlyList<RoomId> activeRoomIds = await _roomRuntimeRepository.GetAllActiveRoomIdsAsync(cancellationToken);
        int totalMutations = 0;

        foreach (RoomId roomId in activeRoomIds)
        {
            totalMutations += await _roomRollerService.ProcessAsync(roomId, cancellationToken);
        }

        return totalMutations;
    }
}
