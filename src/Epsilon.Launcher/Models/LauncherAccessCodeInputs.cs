namespace Epsilon.Launcher;

public sealed record LauncherAccessCodeIssueInput(
    string? Ticket,
    string? PlatformKind);

public sealed record LauncherAccessCodeRedeemInput(
    string? Code,
    string? DeviceLabel,
    string? PlatformKind);
