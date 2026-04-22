namespace Epsilon.CoreGame;

/// <summary>
/// Captures inbound request/response cycles for diagnostic inspection and packet-level auditing.
/// </summary>
public interface IPacketLogger
{
    void Log(PacketLogEntry entry);
    IReadOnlyList<PacketLogEntry> GetRecent(int count = 200);
}
