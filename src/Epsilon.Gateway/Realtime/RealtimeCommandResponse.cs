namespace Epsilon.Gateway;

public sealed record RealtimeCommandResponse(
    string? Id,
    string Command,
    bool Ok,
    int StatusCode,
    object Payload);
