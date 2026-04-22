namespace Epsilon.Protocol;

public sealed class PacketRegistry
{
    private readonly Dictionary<string, int> _incoming;
    private readonly Dictionary<string, int> _outgoing;
    private readonly string _family;

    public PacketRegistry(PacketManifestLoader loader)
    {
        PacketManifest manifest = loader.Load();
        _family = RequireValue(manifest.Family, "Protocol packet family is required.");

        _incoming = BuildRegistry(manifest.Packets, "incoming");
        _outgoing = BuildRegistry(manifest.Packets, "outgoing");
    }

    public string Family => _family;
    public IReadOnlyDictionary<string, int> Incoming => _incoming;
    public IReadOnlyDictionary<string, int> Outgoing => _outgoing;

    public bool ContainsIncoming(string packetName) => _incoming.ContainsKey(packetName);
    public bool ContainsOutgoing(string packetName) => _outgoing.ContainsKey(packetName);

    public bool TryGetIncomingId(string packetName, out int packetId) => _incoming.TryGetValue(packetName, out packetId);
    public bool TryGetOutgoingId(string packetName, out int packetId) => _outgoing.TryGetValue(packetName, out packetId);

    private static Dictionary<string, int> BuildRegistry(IEnumerable<PacketDefinition> packets, string direction)
    {
        Dictionary<string, int> registry = new(StringComparer.OrdinalIgnoreCase);
        HashSet<int> usedPacketIds = [];

        foreach (PacketDefinition packet in packets.Where(packet =>
                     packet.Direction.Equals(direction, StringComparison.OrdinalIgnoreCase)))
        {
            string packetName = RequireValue(packet.Name, $"Protocol packet name is required for direction '{direction}'.");

            if (!usedPacketIds.Add(packet.Id))
            {
                throw new InvalidOperationException(
                    $"Duplicate {direction} packet id '{packet.Id}' was found in the packet manifest.");
            }

            if (!registry.TryAdd(packetName, packet.Id))
            {
                throw new InvalidOperationException(
                    $"Duplicate {direction} packet name '{packetName}' was found in the packet manifest.");
            }
        }

        return registry;
    }

    private static string RequireValue(string? value, string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(errorMessage);
        }

        return value.Trim();
    }
}
