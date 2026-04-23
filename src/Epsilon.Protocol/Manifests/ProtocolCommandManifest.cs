namespace Epsilon.Protocol;

/// <summary>
/// Root command manifest model for one protocol family and revision.
/// </summary>
public sealed class ProtocolCommandManifest
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
    /// Command definitions contained in the manifest.
    /// </summary>
    public List<ProtocolCommandDefinition> Commands { get; init; } = [];
}
