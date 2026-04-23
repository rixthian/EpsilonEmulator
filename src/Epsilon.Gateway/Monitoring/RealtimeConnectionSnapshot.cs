namespace Epsilon.Gateway;

public sealed record RealtimeConnectionSnapshot(
    int ActiveConnections,
    long TotalAcceptedConnections,
    DateTime? LastConnectedAtUtc,
    DateTime? LastDisconnectedAtUtc);
