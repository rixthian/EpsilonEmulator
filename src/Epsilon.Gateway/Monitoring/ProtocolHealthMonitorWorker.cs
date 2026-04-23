namespace Epsilon.Gateway;

public sealed class ProtocolHealthMonitorWorker : BackgroundService
{
    private readonly IProtocolHealthMonitor _protocolHealthMonitor;
    private readonly ProtocolHealthMonitorOptions _options;

    public ProtocolHealthMonitorWorker(
        IProtocolHealthMonitor protocolHealthMonitor,
        Microsoft.Extensions.Options.IOptions<ProtocolHealthMonitorOptions> options)
    {
        _protocolHealthMonitor = protocolHealthMonitor;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _protocolHealthMonitor.Refresh();

        using PeriodicTimer timer = new(TimeSpan.FromSeconds(_options.CheckIntervalSeconds));
        while (!stoppingToken.IsCancellationRequested &&
               await timer.WaitForNextTickAsync(stoppingToken))
        {
            _protocolHealthMonitor.Refresh();
        }
    }
}
