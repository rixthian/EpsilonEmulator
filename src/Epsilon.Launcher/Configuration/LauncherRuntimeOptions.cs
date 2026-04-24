namespace Epsilon.Launcher;

public sealed class LauncherRuntimeOptions
{
    public const string SectionName = "Launcher";

    public string ServiceName { get; set; } = "Epsilon.Launcher";

    public string GatewayBaseUrl { get; set; } = "http://127.0.0.1:5100";
    public string GatewayRealtimePath { get; set; } = "/realtime";

    public string AssetsBaseUrl { get; set; } = "/assets";

    public LauncherDesktopOptions DesktopLauncher { get; set; } = new();

    public List<LauncherClientProfile> ClientProfiles { get; set; } = [];

    public List<LauncherConnectionProfile> ConnectionProfiles { get; set; } = [];
}
