namespace Epsilon.Launcher;

public sealed record LauncherClientProfile(
    string ProfileKey,
    string DisplayName,
    string PackageKey,
    string RendererKind,
    string TransportKind,
    bool SupportsSso,
    bool SupportsDirectLogin,
    bool IsDefault,
    IReadOnlyList<LauncherDeviceKind> SupportedDevices,
    IReadOnlyList<string> Tags);
