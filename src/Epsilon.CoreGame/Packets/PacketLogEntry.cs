namespace Epsilon.CoreGame;

/// <summary>
/// Captures a single request/response cycle for diagnostic and audit purposes.
/// </summary>
public sealed record PacketLogEntry(
    DateTime TimestampUtc,
    string Direction,
    string EndpointName,
    long? CharacterId,
    string? RemoteAddress,
    int ResponseStatusCode,
    long ElapsedMs);
