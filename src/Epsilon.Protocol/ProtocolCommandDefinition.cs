namespace Epsilon.Protocol;

public sealed class ProtocolCommandDefinition
{
    public required string Name { get; init; }
    public required string PacketName { get; init; }
    public required string Flow { get; init; }
    public bool Required { get; init; } = true;
}
