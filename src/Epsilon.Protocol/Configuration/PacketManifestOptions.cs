namespace Epsilon.Protocol;

/// <summary>
/// Configuration for loading protocol packet and command manifests.
/// </summary>
public sealed class PacketManifestOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Protocol";

    /// <summary>
    /// Logical protocol family identifier.
    /// </summary>
    public string Family { get; set; } = string.Empty;
    /// <summary>
    /// Path to the packet manifest file.
    /// </summary>
    public string ManifestPath { get; set; } = string.Empty;
    /// <summary>
    /// Path to the protocol command manifest file.
    /// </summary>
    public string CommandManifestPath { get; set; } = string.Empty;
}
