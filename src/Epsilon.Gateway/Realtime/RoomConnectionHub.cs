using System.Collections.Concurrent;
using Epsilon.CoreGame;

namespace Epsilon.Gateway;

/// <summary>
/// Thread-safe, in-process registry that maps rooms to active WebSocket send delegates.
/// Registered as a singleton so the lifetime matches the Gateway process.
/// </summary>
public sealed class RoomConnectionHub : IRoomConnectionHub
{
    // ConcurrentDictionary<RoomId, ConcurrentDictionary<connectionId, sendFn>>
    private readonly ConcurrentDictionary<RoomId, ConcurrentDictionary<Guid, Func<string, CancellationToken, Task>>> _rooms = new();

    public Guid Register(RoomId roomId, Func<string, CancellationToken, Task> sendFn)
    {
        Guid connectionId = Guid.NewGuid();
        _rooms
            .GetOrAdd(roomId, static _ => new ConcurrentDictionary<Guid, Func<string, CancellationToken, Task>>())
            .TryAdd(connectionId, sendFn);
        return connectionId;
    }

    public void Unregister(RoomId roomId, Guid connectionId)
    {
        if (_rooms.TryGetValue(roomId, out ConcurrentDictionary<Guid, Func<string, CancellationToken, Task>>? connections))
            connections.TryRemove(connectionId, out _);
    }

    public Task BroadcastToRoomAsync(
        RoomId roomId,
        string json,
        Guid excludeConnectionId,
        CancellationToken cancellationToken)
    {
        if (!_rooms.TryGetValue(roomId, out ConcurrentDictionary<Guid, Func<string, CancellationToken, Task>>? connections) ||
            connections.IsEmpty)
        {
            return Task.CompletedTask;
        }

        Task[] tasks = connections
            .Where(pair => pair.Key != excludeConnectionId)
            .Select(pair => TrySendAsync(pair.Value, json, cancellationToken))
            .ToArray();

        return tasks.Length > 0 ? Task.WhenAll(tasks) : Task.CompletedTask;
    }

    private static async Task TrySendAsync(
        Func<string, CancellationToken, Task> sendFn,
        string json,
        CancellationToken cancellationToken)
    {
        try
        {
            await sendFn(json, cancellationToken);
        }
        catch
        {
            // The connection may have closed between broadcast enumeration and delivery.
            // Silently discard — the connection will be unregistered on its own cleanup path.
        }
    }
}
