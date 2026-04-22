namespace Epsilon.Launcher;

public sealed record LauncherConnectionProfile(
    LauncherDeviceKind DeviceKind,
    string TransportKind,
    string ProtocolFamily,
    string InputMode,
    bool SupportsTouchInput,
    bool SupportsSafeReconnect,
    bool RequiresCompactViewport,
    int HeartbeatIntervalSeconds,
    int MaximumViewportWidth,
    int MaximumViewportHeight,
    double PreferredAssetDensity,
    IReadOnlyList<string> EnabledCapabilities);
