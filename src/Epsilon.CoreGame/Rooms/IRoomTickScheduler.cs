namespace Epsilon.CoreGame;

public interface IRoomTickScheduler
{
    ValueTask<int> TickAsync(int rollerIntervalTicks, CancellationToken cancellationToken = default);
}
