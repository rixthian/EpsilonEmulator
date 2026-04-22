namespace Epsilon.Protocol;

public sealed class ProtocolCommandManifest
{
    public required string Family { get; init; }
    public required string Revision { get; init; }
    public List<ProtocolCommandDefinition> Commands { get; init; } = [];
}
