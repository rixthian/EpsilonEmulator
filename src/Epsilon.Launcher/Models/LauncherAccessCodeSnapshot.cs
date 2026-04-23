namespace Epsilon.Launcher;

public sealed record LauncherAccessCodeSnapshot(
    string Code,
    string Ticket,
    string? PlatformKind,
    DateTime IssuedAtUtc,
    DateTime ExpiresAtUtc,
    DateTime? RedeemedAtUtc);
