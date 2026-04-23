namespace Epsilon.Launcher;

public sealed record LauncherTelemetryInput(
    string Ticket,
    string EventKey,
    string? Detail,
    IReadOnlyDictionary<string, string>? Data);
