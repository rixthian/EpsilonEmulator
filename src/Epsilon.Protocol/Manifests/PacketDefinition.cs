namespace Epsilon.Protocol;

public sealed class PacketDefinition
{
    public required string Name { get; init; }
    public required string Direction { get; init; }
    public required int Id { get; init; }
    public string Confidence { get; init; } = "inferred";
    public string Notes { get; init; } = string.Empty;
}

