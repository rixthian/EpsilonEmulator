using Epsilon.CoreGame;
using Microsoft.Extensions.Options;

namespace Epsilon.Gateway;

public sealed class RoomTickWorker : BackgroundService
{
    private readonly IRoomTickScheduler _roomTickScheduler;
    private readonly RoomTickSchedulerOptions _options;

    public RoomTickWorker(
        IRoomTickScheduler roomTickScheduler,
        IOptions<RoomTickSchedulerOptions> options)
    {
        _roomTickScheduler = roomTickScheduler;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(TimeSpan.FromMilliseconds(_options.TickIntervalMilliseconds));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await _roomTickScheduler.TickAsync(_options.RollerIntervalTicks, stoppingToken);
        }
    }
}
