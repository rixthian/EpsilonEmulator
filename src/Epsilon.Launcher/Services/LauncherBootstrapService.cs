using Epsilon.Auth;
using Epsilon.Content;
using Epsilon.CoreGame;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace Epsilon.Launcher;

public sealed class LauncherBootstrapService : ILauncherBootstrapService
{
    private readonly LauncherRuntimeOptions _options;
    private readonly IClientPackageRepository _clientPackageRepository;
    private readonly IInterfacePreferenceService _interfacePreferenceService;
    private readonly ISessionStore _sessionStore;
    private readonly ICollectorProfileService _collectorProfileService;
    private readonly ILaunchEntitlementService _launchEntitlementService;
    private readonly IHttpClientFactory _httpClientFactory;

    public LauncherBootstrapService(
        IOptions<LauncherRuntimeOptions> options,
        IClientPackageRepository clientPackageRepository,
        IInterfacePreferenceService interfacePreferenceService,
        ISessionStore sessionStore,
        ICollectorProfileService collectorProfileService,
        ILaunchEntitlementService launchEntitlementService,
        IHttpClientFactory httpClientFactory)
    {
        _options = options.Value;
        _clientPackageRepository = clientPackageRepository;
        _interfacePreferenceService = interfacePreferenceService;
        _sessionStore = sessionStore;
        _collectorProfileService = collectorProfileService;
        _launchEntitlementService = launchEntitlementService;
        _httpClientFactory = httpClientFactory;
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
        CollectorProfileSnapshot? collector = null;
        LaunchEntitlementSnapshot? launchEntitlement = null;
        if (session is not null)
        {
            CharacterId characterId = new(session.CharacterId);
            preferences = await _interfacePreferenceService.GetSnapshotAsync(characterId, cancellationToken);
            collector = await _collectorProfileService.BuildAsync(characterId, cancellationToken);
            launchEntitlement = await _launchEntitlementService.EvaluateAsync(characterId, cancellationToken);

            if (!string.IsNullOrWhiteSpace(sessionTicket))
            {
                collector = await ResolveGatewayCollectorAsync(sessionTicket.Trim(), cancellationToken) ?? collector;
                launchEntitlement = await ResolveGatewayLaunchEntitlementAsync(sessionTicket.Trim(), cancellationToken) ?? launchEntitlement;
            }
        }

        return new LauncherBootstrapSnapshot(
            Profile: MapProfile(profile),
            ConnectionPolicy: connectionPolicy,
            Package: package,
            GatewayBaseUrl: _options.GatewayBaseUrl,
            EntryAssetUrl: BuildAssetUrl(package.EntryAssetPath),
            AssetBaseUrl: BuildAssetUrl(package.AssetBasePath),
            InterfacePreferences: preferences,
            Collector: collector,
            LaunchEntitlement: launchEntitlement,
            SupportedLanguages: preferences?.SupportedLanguages ?? [],
            Session: session is null
                ? null
                : new LauncherSessionSnapshot(
                    session.AccountId,
                    session.CharacterId,
                    session.Ticket,
                    session.ExpiresAtUtc,
                    collector,
                    launchEntitlement),
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
        session ??= await ResolveGatewaySessionAsync(sessionTicket.Trim(), cancellationToken);
        if (session is null || session.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return null;
        }

        return session;
    }

    private async ValueTask<SessionTicket?> ResolveGatewaySessionAsync(
        string sessionTicket,
        CancellationToken cancellationToken)
    {
        using HttpClient httpClient = CreateGatewayHttpClient();
        if (httpClient.BaseAddress is null)
        {
            return null;
        }

        try
        {
            return await httpClient.GetFromJsonAsync<SessionTicket>(
                $"/auth/development/sessions/{Uri.EscapeDataString(sessionTicket)}",
                cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    private async ValueTask<CollectorProfileSnapshot?> ResolveGatewayCollectorAsync(
        string sessionTicket,
        CancellationToken cancellationToken)
    {
        using HttpClient httpClient = CreateGatewayHttpClient();
        if (httpClient.BaseAddress is null)
        {
            return null;
        }

        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-Epsilon-Session-Ticket", sessionTicket);

        try
        {
            return await httpClient.GetFromJsonAsync<CollectorProfileSnapshot>(
                "/hotel/collectibles/profile",
                cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    private async ValueTask<LaunchEntitlementSnapshot?> ResolveGatewayLaunchEntitlementAsync(
        string sessionTicket,
        CancellationToken cancellationToken)
    {
        using HttpClient httpClient = CreateGatewayHttpClient();
        if (httpClient.BaseAddress is null)
        {
            return null;
        }

        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-Epsilon-Session-Ticket", sessionTicket);

        try
        {
            return await httpClient.GetFromJsonAsync<LaunchEntitlementSnapshot>(
                "/hotel/collectibles/launch-access",
                cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    private HttpClient CreateGatewayHttpClient()
    {
        HttpClient httpClient = _httpClientFactory.CreateClient();
        if (!string.IsNullOrWhiteSpace(_options.GatewayBaseUrl))
        {
            httpClient.BaseAddress = new Uri(_options.GatewayBaseUrl, UriKind.Absolute);
        }

        return httpClient;
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
            ["realtime"] = BuildRealtimeUrl(gatewayBase),
            ["collectorProfile"] = $"{gatewayBase}/hotel/collectibles/profile",
            ["collectorProgression"] = $"{gatewayBase}/hotel/collectibles/progression",
            ["launchAccess"] = $"{gatewayBase}/hotel/collectibles/launch-access",
            ["walletChallenge"] = $"{gatewayBase}/hotel/collectibles/wallet/challenges",
            ["walletVerify"] = $"{gatewayBase}/hotel/collectibles/wallet/verify",
            ["emeraldAccrual"] = $"{gatewayBase}/hotel/collectibles/emeralds/accrue",
            ["giftBoxes"] = $"{gatewayBase}/hotel/collectibles/gift-boxes",
            ["factories"] = $"{gatewayBase}/hotel/collectibles/factories",
            ["collectimaticRecipes"] = $"{gatewayBase}/hotel/collectibles/collectimatic/recipes",
            ["marketplaceListings"] = $"{gatewayBase}/hotel/collectibles/marketplace/listings",
            ["publicCollectibles"] = $"{gatewayBase}/api/public/collectibles",
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

    private string BuildRealtimeUrl(string gatewayBase)
    {
        string realtimeBase = gatewayBase.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? $"wss://{gatewayBase["https://".Length..]}"
            : gatewayBase.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                ? $"ws://{gatewayBase["http://".Length..]}"
                : gatewayBase;

        return $"{realtimeBase.TrimEnd('/')}/{_options.GatewayRealtimePath.TrimStart('/')}";
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
