namespace Epsilon.Launcher;

public sealed class LauncherDesktopOptions
{
    public string HotelBaseUrl { get; set; } = "http://localhost:8081";

    public string LauncherApiBaseUrl { get; set; } = "http://localhost:5001";

    public string DefaultChannel { get; set; } = "stable";

    public string DefaultProfileKey { get; set; } = "loader-desktop";

    public LauncherLocalConfigDefaultsOptions LocalConfigDefaults { get; set; } = new();

    public List<LauncherUpdateChannelOptions> UpdateChannels { get; set; } = [];

    public List<LauncherDesktopProfileOptions> LaunchProfiles { get; set; } = [];
}

public sealed class LauncherLocalConfigDefaultsOptions
{
    public bool AutoLaunchOnRedeem { get; set; } = true;

    public bool CloseCmsOnLaunch { get; set; }

    public bool HardwareAcceleration { get; set; } = true;

    public bool TelemetryEnabled { get; set; } = true;

    public bool RememberLastProfile { get; set; } = true;
}

public sealed class LauncherUpdateChannelOptions
{
    public string ChannelKey { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string LauncherManifestUrl { get; set; } = string.Empty;

    public string PackageManifestUrl { get; set; } = string.Empty;

    public bool AllowDowngrade { get; set; }

    public bool RequiresSignedPackages { get; set; } = true;
}

public sealed class LauncherDesktopProfileOptions
{
    public string ProfileKey { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Channel { get; set; } = "stable";

    public List<string> Platforms { get; set; } = [];

    public string ClientKind { get; set; } = string.Empty;

    public string PackageKey { get; set; } = string.Empty;

    public string EntryExecutable { get; set; } = string.Empty;

    public List<string> Arguments { get; set; } = [];

    public bool RequiresRedeemCode { get; set; } = true;

    public bool SupportsSafeReconnect { get; set; } = true;

    public bool SupportsOverlayTelemetry { get; set; }

    public bool IsDefault { get; set; }
}
