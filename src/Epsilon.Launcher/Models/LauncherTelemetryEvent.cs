namespace Epsilon.Launcher;

public sealed record LauncherTelemetryEvent(
    string Ticket,
    string EventKey,
    string? Detail,
    DateTime OccurredAtUtc,
    IReadOnlyDictionary<string, string> Data);
