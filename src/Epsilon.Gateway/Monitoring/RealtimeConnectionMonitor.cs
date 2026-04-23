namespace Epsilon.Gateway;

public sealed class RealtimeConnectionMonitor : IRealtimeConnectionMonitor
{
    private long _activeConnections;
    private long _totalAcceptedConnections;
    private DateTime? _lastConnectedAtUtc;
    private DateTime? _lastDisconnectedAtUtc;

    public void RecordAcceptedConnection()
    {
        Interlocked.Increment(ref _activeConnections);
        Interlocked.Increment(ref _totalAcceptedConnections);
        _lastConnectedAtUtc = DateTime.UtcNow;
    }

    public void RecordClosedConnection()
    {
        long current;
        do
        {
            current = Interlocked.Read(ref _activeConnections);
            if (current == 0)
            {
                break;
            }
        }
        while (Interlocked.CompareExchange(ref _activeConnections, current - 1, current) != current);

        _lastDisconnectedAtUtc = DateTime.UtcNow;
    }

    public RealtimeConnectionSnapshot GetSnapshot()
    {
        return new RealtimeConnectionSnapshot(
            ActiveConnections: (int)Interlocked.Read(ref _activeConnections),
            TotalAcceptedConnections: Interlocked.Read(ref _totalAcceptedConnections),
            LastConnectedAtUtc: _lastConnectedAtUtc,
            LastDisconnectedAtUtc: _lastDisconnectedAtUtc);
    }
}
