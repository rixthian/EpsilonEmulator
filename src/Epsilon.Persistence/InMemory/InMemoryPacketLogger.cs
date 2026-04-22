using Epsilon.CoreGame;

namespace Epsilon.Persistence;

/// <summary>
/// Thread-safe fixed-size ring buffer that stores the N most recent packet log entries.
/// Old entries are automatically overwritten when the buffer is full.
/// </summary>
public sealed class InMemoryPacketLogger : IPacketLogger
{
    private const int Capacity = 10_000;
    private readonly PacketLogEntry?[] _ring = new PacketLogEntry?[Capacity];
    private long _head;
    private readonly object _sync = new();

    public void Log(PacketLogEntry entry)
    {
        lock (_sync)
        {
            _ring[_head % Capacity] = entry;
            _head++;
        }
    }

    public IReadOnlyList<PacketLogEntry> GetRecent(int count = 200)
    {
        if (count <= 0)
        {
            return [];
        }

        lock (_sync)
        {
            long available = Math.Min(_head, Capacity);
            int take = (int)Math.Min(count, available);
            PacketLogEntry[] result = new PacketLogEntry[take];
            long startHead = _head - take;

            for (int i = 0; i < take; i++)
            {
                result[i] = _ring[(startHead + i) % Capacity]!;
            }

            return result;
        }
    }
}
