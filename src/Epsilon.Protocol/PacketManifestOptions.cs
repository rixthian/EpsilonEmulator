namespace Epsilon.Protocol;

public sealed class PacketManifestOptions
{
    public const string SectionName = "Protocol";

    public string Family { get; set; } = string.Empty;
    public string ManifestPath { get; set; } = string.Empty;
}
