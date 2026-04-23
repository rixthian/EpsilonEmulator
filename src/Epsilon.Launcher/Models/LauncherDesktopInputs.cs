namespace Epsilon.Launcher;

public sealed record LauncherProfileSelectionInput(
    string? Ticket,
    string? ProfileKey,
    string? PlatformKind);

public sealed record LauncherClientStartedInput(
    string? Ticket,
    string? ProfileKey,
    string? ClientKind,
    string? PlatformKind);
