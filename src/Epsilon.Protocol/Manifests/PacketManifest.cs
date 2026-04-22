namespace Epsilon.Protocol;

public sealed class PacketManifest
{
    public required string Family { get; init; }
    public required string Revision { get; init; }
    public List<PacketDefinition> Packets { get; init; } = [];
}

