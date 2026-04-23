namespace Epsilon.Protocol;

/// <summary>
/// Result of a protocol manifest self-check pass.
/// </summary>
public sealed record ProtocolSelfCheckReport
{
    /// <summary>
    /// Indicates whether the protocol manifests are internally consistent.
    /// </summary>
    public required bool IsHealthy { get; init; }

    /// <summary>
    /// Packet manifest family.
    /// </summary>
    public required string PacketFamily { get; init; }

    /// <summary>
    /// Command manifest family.
    /// </summary>
    public required string CommandFamily { get; init; }

    /// <summary>
    /// Command manifest revision.
    /// </summary>
    public required string CommandRevision { get; init; }

    /// <summary>
    /// Number of incoming packets in the registry.
    /// </summary>
    public required int IncomingPacketCount { get; init; }

    /// <summary>
    /// Number of outgoing packets in the registry.
    /// </summary>
    public required int OutgoingPacketCount { get; init; }

    /// <summary>
    /// Number of commands in the registry.
    /// </summary>
    public required int CommandCount { get; init; }

    /// <summary>
    /// Reported validation issues.
    /// </summary>
    public required IReadOnlyList<string> Issues { get; init; }
}
