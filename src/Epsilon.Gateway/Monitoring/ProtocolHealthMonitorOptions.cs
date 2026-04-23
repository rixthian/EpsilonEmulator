namespace Epsilon.Gateway;

public sealed class ProtocolHealthMonitorOptions
{
    public const string SectionName = "ProtocolHealthMonitor";

    public int CheckIntervalSeconds { get; set; } = 30;
    public int RecentPacketWindowSeconds { get; set; } = 300;
    public int IdleWarningSeconds { get; set; } = 180;
    public int IdleCriticalSeconds { get; set; } = 600;
    public int RealtimeIdleWarningSeconds { get; set; } = 300;
    public int RealtimeIdleCriticalSeconds { get; set; } = 900;
}
