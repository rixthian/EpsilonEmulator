using Epsilon.Auth;
using Epsilon.Content;
using Epsilon.CoreGame;
using Microsoft.Extensions.Options;

namespace Epsilon.Launcher;

public sealed class LauncherBootstrapService : ILauncherBootstrapService
{
    private readonly LauncherRuntimeOptions _options;
    private readonly IClientPackageRepository _clientPackageRepository;
    private readonly IInterfacePreferenceService _interfacePreferenceService;
    private readonly ISessionStore _sessionStore;

    public LauncherBootstrapService(
        IOptions<LauncherRuntimeOptions> options,
        IClientPackageRepository clientPackageRepository,
        IInterfacePreferenceService interfacePreferenceService,
        ISessionStore sessionStore)
    {
        _options = options.Value;
        _clientPackageRepository = clientPackageRepository;
        _interfacePreferenceService = interfacePreferenceService;
        _sessionStore = sessionStore;
    }

    public ValueTask<IReadOnlyList<LauncherClientProfileSnapshot>> GetProfilesAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<LauncherClientProfileSnapshot> profiles = _options.ClientProfiles
            .OrderByDescending(profile => profile.IsDefault)
            .ThenBy(profile => profile.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(MapProfile)
            .ToArray();

        return ValueTask.FromResult(profiles);
    }

    public async ValueTask<LauncherBootstrapSnapshot?> BuildAsync(
        string profileKey,
        string? sessionTicket,
        string? userAgent,
        LauncherDeviceKind? requestedDeviceKind,
        CancellationToken cancellationToken = default)
    {
        LauncherClientProfile? profile = ResolveProfile(profileKey);
        if (profile is null)
        {
            return null;
        }

        LauncherDeviceKind resolvedDeviceKind = ResolveDeviceKind(userAgent, requestedDeviceKind);
        if (!profile.SupportedDevices.Contains(resolvedDeviceKind))
        {
            resolvedDeviceKind = profile.SupportedDevices.FirstOrDefault();
        }

        LauncherConnectionPolicy connectionPolicy = ResolveConnectionPolicy(resolvedDeviceKind);

        ClientPackageManifest? package = await _clientPackageRepository.GetByKeyAsync(profile.PackageKey, cancellationToken);
        if (package is null)
        {
            return null;
        }

        SessionTicket? session = await ResolveSessionAsync(sessionTicket, cancellationToken);
        InterfacePreferenceSnapshot? preferences = null;
        if (session is not null)
        {
            preferences = await _interfacePreferenceService.GetSnapshotAsync(new CharacterId(session.CharacterId), cancellationToken);
        }

        return new LauncherBootstrapSnapshot(
            Profile: MapProfile(profile),
            ConnectionPolicy: connectionPolicy,
            Package: package,
            GatewayBaseUrl: _options.GatewayBaseUrl,
            EntryAssetUrl: BuildAssetUrl(package.EntryAssetPath),
            AssetBaseUrl: BuildAssetUrl(package.AssetBasePath),
            InterfacePreferences: preferences,
            SupportedLanguages: preferences?.SupportedLanguages ?? [],
            Session: session is null
                ? null
                : new LauncherSessionSnapshot(session.AccountId, session.CharacterId, session.Ticket, session.ExpiresAtUtc),
            EndpointMap: BuildEndpointMap());
    }

    private LauncherClientProfile? ResolveProfile(string profileKey)
    {
        if (string.IsNullOrWhiteSpace(profileKey))
        {
            return _options.ClientProfiles.FirstOrDefault(profile => profile.IsDefault);
        }

        return _options.ClientProfiles.FirstOrDefault(profile =>
            string.Equals(profile.ProfileKey, profileKey, StringComparison.OrdinalIgnoreCase));
    }

    private async ValueTask<SessionTicket?> ResolveSessionAsync(
        string? sessionTicket,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sessionTicket))
        {
            return null;
        }

        SessionTicket? session = await _sessionStore.FindByTicketAsync(sessionTicket.Trim(), cancellationToken);
        if (session is null || session.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return null;
        }

        return session;
    }

    private string BuildAssetUrl(string relativePath)
    {
        string trimmedBase = _options.AssetsBaseUrl.TrimEnd('/');
        string trimmedPath = relativePath.TrimStart('/');
        return $"{trimmedBase}/{trimmedPath}";
    }

    private IReadOnlyDictionary<string, string> BuildEndpointMap()
    {
        string gatewayBase = _options.GatewayBaseUrl.TrimEnd('/');
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["health"] = $"{gatewayBase}/health",
            ["catalogPages"] = $"{gatewayBase}/hotel/catalog/pages",
            ["catalogLanding"] = $"{gatewayBase}/hotel/catalog/landing",
            ["inventory"] = $"{gatewayBase}/hotel/inventory",
            ["roomEntry"] = $"{gatewayBase}/hotel/rooms/entry",
            ["roomMove"] = $"{gatewayBase}/hotel/rooms/move",
            ["roomChat"] = $"{gatewayBase}/hotel/rooms/chat",
            ["games"] = $"{gatewayBase}/hotel/games",
            ["preferences"] = $"{gatewayBase}/hotel/preferences/interface"
        };
    }

    private static LauncherClientProfileSnapshot MapProfile(LauncherClientProfile profile)
    {
        return new LauncherClientProfileSnapshot(
            profile.ProfileKey,
            profile.DisplayName,
            profile.PackageKey,
            profile.RendererKind,
            profile.TransportKind,
            profile.SupportsSso,
            profile.SupportsDirectLogin,
            profile.IsDefault,
            profile.SupportedDevices,
            profile.Tags);
    }

    private LauncherDeviceKind ResolveDeviceKind(
        string? userAgent,
        LauncherDeviceKind? requestedDeviceKind)
    {
        if (requestedDeviceKind is not null)
        {
            return requestedDeviceKind.Value;
        }

        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return LauncherDeviceKind.Desktop;
        }

        string normalized = userAgent.Trim();
        if (normalized.Contains("iPad", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("Tablet", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("Android", StringComparison.OrdinalIgnoreCase) && !normalized.Contains("Mobile", StringComparison.OrdinalIgnoreCase))
        {
            return LauncherDeviceKind.Tablet;
        }

        if (normalized.Contains("iPhone", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("Mobile", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("Android", StringComparison.OrdinalIgnoreCase))
        {
            return LauncherDeviceKind.Phone;
        }

        return LauncherDeviceKind.Desktop;
    }

    private LauncherConnectionPolicy ResolveConnectionPolicy(LauncherDeviceKind deviceKind)
    {
        LauncherConnectionProfile? profile = _options.ConnectionProfiles.FirstOrDefault(candidate => candidate.DeviceKind == deviceKind)
            ?? _options.ConnectionProfiles.FirstOrDefault(candidate => candidate.DeviceKind == LauncherDeviceKind.Desktop);

        if (profile is null)
        {
            return new LauncherConnectionPolicy(
                deviceKind,
                "epsilon_runtime_api",
                "epsilon-mobile",
                deviceKind is LauncherDeviceKind.Desktop ? "pointer_keyboard" : "touch",
                deviceKind is LauncherDeviceKind.Phone or LauncherDeviceKind.Tablet,
                true,
                deviceKind is not LauncherDeviceKind.Desktop,
                20,
                deviceKind is LauncherDeviceKind.Phone ? 480 : 1280,
                deviceKind is LauncherDeviceKind.Phone ? 932 : 900,
                deviceKind is LauncherDeviceKind.Phone ? 2.0 : 1.0,
                ["session-resume", "touch-navigation", "adaptive-scaling"]);
        }

        return new LauncherConnectionPolicy(
            profile.DeviceKind,
            profile.TransportKind,
            profile.ProtocolFamily,
            profile.InputMode,
            profile.SupportsTouchInput,
            profile.SupportsSafeReconnect,
            profile.RequiresCompactViewport,
            profile.HeartbeatIntervalSeconds,
            profile.MaximumViewportWidth,
            profile.MaximumViewportHeight,
            profile.PreferredAssetDensity,
            profile.EnabledCapabilities);
    }
}
