namespace Epsilon.Protocol;

/// <summary>
/// Describes one packet entry inside a packet manifest.
/// </summary>
public sealed class PacketDefinition
{
    /// <summary>
    /// Canonical packet name.
    /// </summary>
    public required string Name { get; init; }
    /// <summary>
    /// Packet direction, such as incoming or outgoing.
    /// </summary>
    public required string Direction { get; init; }
    /// <summary>
    /// Packet numeric identifier.
    /// </summary>
    public required int Id { get; init; }
    /// <summary>
    /// Confidence label for the mapping.
    /// </summary>
    public string Confidence { get; init; } = "inferred";
    /// <summary>
    /// Optional operator-facing notes for the packet mapping.
    /// </summary>
    public string Notes { get; init; } = string.Empty;
}
