using System.Collections.Concurrent;

namespace Epsilon.Launcher;

public sealed class LauncherTelemetryStore
{
    private const int MaxEventsPerTicket = 120;

    private readonly ConcurrentDictionary<string, ConcurrentQueue<LauncherTelemetryEvent>> _eventsByTicket =
        new(StringComparer.Ordinal);

    public void Append(LauncherTelemetryEvent telemetryEvent)
    {
        ConcurrentQueue<LauncherTelemetryEvent> queue = _eventsByTicket.GetOrAdd(
            telemetryEvent.Ticket,
            static _ => new ConcurrentQueue<LauncherTelemetryEvent>());

        queue.Enqueue(telemetryEvent);
        while (queue.Count > MaxEventsPerTicket && queue.TryDequeue(out _))
        {
        }
    }

    public IReadOnlyList<LauncherTelemetryEvent> GetByTicket(string ticket)
    {
        if (!_eventsByTicket.TryGetValue(ticket, out ConcurrentQueue<LauncherTelemetryEvent>? queue))
        {
            return [];
        }

        return queue.ToArray()
            .OrderBy(candidate => candidate.OccurredAtUtc)
            .ToArray();
    }
}
