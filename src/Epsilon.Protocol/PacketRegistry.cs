namespace Epsilon.Protocol;

public sealed class PacketRegistry
{
    private readonly Dictionary<string, int> _incoming;
    private readonly Dictionary<string, int> _outgoing;
    private readonly string _family;

    public PacketRegistry(PacketManifestLoader loader)
    {
        PacketManifest manifest = loader.Load();
        _family = manifest.Family;

        _incoming = manifest.Packets
            .Where(packet => packet.Direction.Equals("incoming", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(packet => packet.Name, packet => packet.Id, StringComparer.OrdinalIgnoreCase);

        _outgoing = manifest.Packets
            .Where(packet => packet.Direction.Equals("outgoing", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(packet => packet.Name, packet => packet.Id, StringComparer.OrdinalIgnoreCase);
    }

    public string Family => _family;
    public IReadOnlyDictionary<string, int> Incoming => _incoming;
    public IReadOnlyDictionary<string, int> Outgoing => _outgoing;
}
