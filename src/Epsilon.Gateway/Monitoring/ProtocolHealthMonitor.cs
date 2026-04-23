using Epsilon.CoreGame;
using Epsilon.Protocol;
using Microsoft.Extensions.Options;

namespace Epsilon.Gateway;

public sealed class ProtocolHealthMonitor : IProtocolHealthMonitor
{
    private readonly ProtocolSelfCheckService _protocolSelfCheckService;
    private readonly IPacketLogger _packetLogger;
    private readonly IRealtimeConnectionMonitor _realtimeConnectionMonitor;
    private readonly ProtocolHealthMonitorOptions _options;
    private readonly ILogger<ProtocolHealthMonitor> _logger;
    private readonly DateTime _startedAtUtc = DateTime.UtcNow;
    private readonly Queue<ProtocolHealthAlertRecord> _alertHistory = new();
    private readonly object _sync = new();
    private ProtocolHealthSnapshot _snapshot;
    private ProtocolHealthState _lastAnnouncedState = ProtocolHealthState.Unknown;

    public ProtocolHealthMonitor(
        ProtocolSelfCheckService protocolSelfCheckService,
        IPacketLogger packetLogger,
        IRealtimeConnectionMonitor realtimeConnectionMonitor,
        IOptions<ProtocolHealthMonitorOptions> options,
        ILogger<ProtocolHealthMonitor> logger)
    {
        _protocolSelfCheckService = protocolSelfCheckService;
        _packetLogger = packetLogger;
        _realtimeConnectionMonitor = realtimeConnectionMonitor;
        _options = options.Value;
        _logger = logger;
        _snapshot = BuildSnapshot();
    }

    public ProtocolHealthSnapshot GetSnapshot()
    {
        lock (_sync)
        {
            return _snapshot with
            {
                AlertHistory = _alertHistory.ToArray()
            };
        }
    }

    public void Refresh()
    {
        ProtocolHealthSnapshot snapshot = BuildSnapshot();

        lock (_sync)
        {
            _snapshot = snapshot with
            {
                AlertHistory = _alertHistory.ToArray()
            };
        }

        if (snapshot.State == _lastAnnouncedState)
        {
            return;
        }

        _lastAnnouncedState = snapshot.State;
        RecordAlert(snapshot);
        Announce(snapshot);
    }

    private ProtocolHealthSnapshot BuildSnapshot()
    {
        DateTime now = DateTime.UtcNow;
        ProtocolSelfCheckReport selfCheck = _protocolSelfCheckService.Run();
        RealtimeConnectionSnapshot realtimeConnections = _realtimeConnectionMonitor.GetSnapshot();
        IReadOnlyList<PacketLogEntry> recentEntries = _packetLogger.GetRecent(2_000);

        DateTime windowStart = now.AddSeconds(-_options.RecentPacketWindowSeconds);
        PacketLogEntry[] windowEntries = recentEntries
            .Where(entry => entry.TimestampUtc >= windowStart)
            .OrderBy(entry => entry.TimestampUtc)
            .ToArray();

        DateTime? lastPacketAtUtc = windowEntries.LastOrDefault()?.TimestampUtc
            ?? recentEntries.OrderBy(entry => entry.TimestampUtc).LastOrDefault()?.TimestampUtc;
        int recentServerErrorCount = windowEntries.Count(entry => entry.ResponseStatusCode >= StatusCodes.Status500InternalServerError);

        List<string> alerts = [];
        ProtocolHealthState state = ProtocolHealthState.Healthy;

        if (!selfCheck.IsHealthy)
        {
            state = ProtocolHealthState.Critical;
            alerts.AddRange(selfCheck.Issues);
        }

        if (recentServerErrorCount > 0)
        {
            state = Max(state, ProtocolHealthState.Warning);
            alerts.Add($"Recent protocol traffic includes {recentServerErrorCount} server error responses.");
        }

        if (lastPacketAtUtc is not null)
        {
            double idleSeconds = (now - lastPacketAtUtc.Value).TotalSeconds;
            if (idleSeconds >= _options.IdleCriticalSeconds)
            {
                state = Max(state, ProtocolHealthState.Critical);
                alerts.Add($"No protocol traffic has been observed for {Math.Round(idleSeconds)} seconds.");
            }
            else if (idleSeconds >= _options.IdleWarningSeconds)
            {
                state = Max(state, ProtocolHealthState.Warning);
                alerts.Add($"Protocol traffic has been idle for {Math.Round(idleSeconds)} seconds.");
            }
        }
        else
        {
            alerts.Add("No protocol traffic has been observed yet.");
            if ((now - _startedAtUtc).TotalSeconds >= _options.IdleWarningSeconds)
            {
                state = Max(state, ProtocolHealthState.Warning);
            }
        }

        if (realtimeConnections.ActiveConnections == 0 && realtimeConnections.TotalAcceptedConnections > 0)
        {
            DateTime? lastRealtimeAtUtc = realtimeConnections.LastDisconnectedAtUtc ?? realtimeConnections.LastConnectedAtUtc;
            if (lastRealtimeAtUtc is not null)
            {
                double realtimeIdleSeconds = (now - lastRealtimeAtUtc.Value).TotalSeconds;
                if (realtimeIdleSeconds >= _options.RealtimeIdleCriticalSeconds)
                {
                    state = Max(state, ProtocolHealthState.Critical);
                    alerts.Add($"Realtime transport has had no active connections for {Math.Round(realtimeIdleSeconds)} seconds.");
                }
                else if (realtimeIdleSeconds >= _options.RealtimeIdleWarningSeconds)
                {
                    state = Max(state, ProtocolHealthState.Warning);
                    alerts.Add($"Realtime transport has been idle for {Math.Round(realtimeIdleSeconds)} seconds.");
                }
            }
        }

        return new ProtocolHealthSnapshot(
            state,
            now,
            _startedAtUtc,
            selfCheck,
            windowEntries.Length,
            recentServerErrorCount,
            lastPacketAtUtc,
            alerts,
            []);
    }

    private void Announce(ProtocolHealthSnapshot snapshot)
    {
        string message = snapshot.State switch
        {
            ProtocolHealthState.Healthy => "protocol monitor healthy",
            ProtocolHealthState.Warning => $"protocol monitor warning: {string.Join(" ", snapshot.Alerts)}",
            ProtocolHealthState.Critical => $"protocol monitor critical: {string.Join(" ", snapshot.Alerts)}",
            _ => "protocol monitor state unknown"
        };

        GatewayConsoleEventKind kind = snapshot.State switch
        {
            ProtocolHealthState.Healthy => GatewayConsoleEventKind.Ok,
            ProtocolHealthState.Warning => GatewayConsoleEventKind.Warning,
            ProtocolHealthState.Critical => GatewayConsoleEventKind.Alert,
            _ => GatewayConsoleEventKind.Info
        };

        GatewayConsole.WriteEvent(kind, message);
        _logger.LogInformation("Protocol monitor state changed to {State}.", snapshot.State);
    }

    private static ProtocolHealthState Max(ProtocolHealthState left, ProtocolHealthState right)
    {
        return (ProtocolHealthState)Math.Max((int)left, (int)right);
    }

    private void RecordAlert(ProtocolHealthSnapshot snapshot)
    {
        string message = snapshot.Alerts.Count == 0
            ? $"Protocol health changed to {snapshot.State}."
            : string.Join(" ", snapshot.Alerts);

        lock (_sync)
        {
            _alertHistory.Enqueue(new ProtocolHealthAlertRecord(snapshot.CheckedAtUtc, snapshot.State, message));
            while (_alertHistory.Count > 50)
            {
                _alertHistory.Dequeue();
            }

            _snapshot = _snapshot with
            {
                AlertHistory = _alertHistory.ToArray()
            };
        }
    }
}
