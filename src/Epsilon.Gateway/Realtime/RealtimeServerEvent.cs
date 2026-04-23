namespace Epsilon.Gateway;

public sealed record RealtimeServerEvent(
    string Event,
    object Payload);
