namespace Epsilon.Protocol;

/// <summary>
/// Root packet manifest model for one protocol family and revision.
/// </summary>
public sealed class PacketManifest
{
    /// <summary>
    /// Protocol family name.
    /// </summary>
    public required string Family { get; init; }
    /// <summary>
    /// Manifest revision identifier.
    /// </summary>
    public required string Revision { get; init; }
    /// <summary>
    /// Packet definitions contained in the manifest.
    /// </summary>
    public List<PacketDefinition> Packets { get; init; } = [];
}
