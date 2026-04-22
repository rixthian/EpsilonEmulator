namespace Epsilon.Launcher;

public interface ILauncherBootstrapService
{
    ValueTask<IReadOnlyList<LauncherClientProfileSnapshot>> GetProfilesAsync(
        CancellationToken cancellationToken = default);

    ValueTask<LauncherBootstrapSnapshot?> BuildAsync(
        string profileKey,
        string? sessionTicket,
        string? userAgent,
        LauncherDeviceKind? requestedDeviceKind,
        CancellationToken cancellationToken = default);
}
