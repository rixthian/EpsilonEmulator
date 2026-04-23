namespace Epsilon.Protocol;

/// <summary>
/// Describes one protocol command binding.
/// </summary>
public sealed class ProtocolCommandDefinition
{
    /// <summary>
    /// Canonical command name.
    /// </summary>
    public required string Name { get; init; }
    /// <summary>
    /// Incoming packet name that activates the command.
    /// </summary>
    public required string PacketName { get; init; }
    /// <summary>
    /// Logical gameplay or handshake flow the command belongs to.
    /// </summary>
    public required string Flow { get; init; }
    /// <summary>
    /// Indicates whether the command must exist for the selected protocol family.
    /// </summary>
    public bool Required { get; init; } = true;
}
