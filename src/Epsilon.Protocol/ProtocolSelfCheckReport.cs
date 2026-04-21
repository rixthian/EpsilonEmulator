namespace Epsilon.Protocol;

public sealed record ProtocolSelfCheckReport(
    bool IsHealthy,
    string PacketFamily,
    string CommandFamily,
    string CommandRevision,
    int IncomingPacketCount,
    int OutgoingPacketCount,
    int CommandCount,
    IReadOnlyList<string> Issues);
