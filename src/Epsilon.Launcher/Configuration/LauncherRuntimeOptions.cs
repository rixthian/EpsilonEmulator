namespace Epsilon.Launcher;

public sealed class LauncherRuntimeOptions
{
    public const string SectionName = "Launcher";

    public string ServiceName { get; set; } = "Epsilon.Launcher";

    public string GatewayBaseUrl { get; set; } = "http://localhost:5000";

    public string AssetsBaseUrl { get; set; } = "/assets";

    public List<LauncherClientProfile> ClientProfiles { get; set; } = [];

    public List<LauncherConnectionProfile> ConnectionProfiles { get; set; } = [];
}
