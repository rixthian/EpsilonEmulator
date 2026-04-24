using Epsilon.Auth;
using Epsilon.CoreGame;
using Epsilon.Games;
using Epsilon.Launcher;
using Epsilon.Persistence;
using Microsoft.Extensions.FileProviders;
using System.Reflection;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

const string SessionTicketHeaderName = "X-Epsilon-Session-Ticket";

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

AddRootConfiguration(builder.Configuration, builder.Environment.EnvironmentName, "launcher");
builder.Configuration.AddEnvironmentVariables(prefix: "EPSILON_");

builder.Services.AddLauncherRuntime(builder.Configuration);
builder.Services.AddPersistenceRuntime(builder.Configuration);
builder.Services.AddAuthRuntime(builder.Configuration);
builder.Services.AddGameRuntime();
builder.Services.AddCoreGameRuntime();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<LauncherTelemetryStore>();
builder.Services.AddSingleton<LauncherAccessCodeStore>();

var app = builder.Build();

string launcherAssetsRoot = Path.Combine(app.Environment.ContentRootPath, "Assets");
if (Directory.Exists(launcherAssetsRoot))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(launcherAssetsRoot),
        RequestPath = "/assets",
        OnPrepareResponse = context =>
        {
            context.Context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
            context.Context.Response.Headers.Pragma = "no-cache";
        }
    });
}

app.MapGet("/launcher/loader", (HttpContext httpContext, string? ticket) =>
{
    string safeTicket = string.IsNullOrWhiteSpace(ticket) ? string.Empty : ticket.Trim();
    bool macLauncherReady = File.Exists(ResolveNativeLauncherMacDmgPath(app.Environment.ContentRootPath));
    httpContext.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
    httpContext.Response.Headers.Pragma = "no-cache";
    return Results.Content(RenderLauncherLoaderPage(safeTicket, macLauncherReady), "text/html; charset=utf-8", Encoding.UTF8);
});

app.MapGet("/launcher/play", (HttpContext httpContext, string? ticket, long? roomId) =>
{
    string safeTicket = string.IsNullOrWhiteSpace(ticket) ? string.Empty : ticket.Trim();
    string location = $"/launcher/loader?ticket={Uri.EscapeDataString(safeTicket)}";
    httpContext.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
    httpContext.Response.Headers.Pragma = "no-cache";
    return Results.Redirect(location, permanent: false);
});

app.MapGet("/launcher/downloads/macos-arm64", (HttpContext httpContext) =>
{
    string dmgPath = ResolveNativeLauncherMacDmgPath(app.Environment.ContentRootPath);
    if (!File.Exists(dmgPath))
    {
        httpContext.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        httpContext.Response.Headers.Pragma = "no-cache";
        return Results.NotFound(new { error = "launcher_package_not_found" });
    }

    httpContext.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
    httpContext.Response.Headers.Pragma = "no-cache";
    return Results.File(dmgPath, "application/x-apple-diskimage", "EpsilonLauncher-macOS-arm64.dmg");
});

app.MapGet("/health", (Microsoft.Extensions.Options.IOptions<LauncherRuntimeOptions> launcherOptions) => Results.Ok(new
{
    service = launcherOptions.Value.ServiceName,
    status = "ok",
    version = ResolveInformationalVersion(typeof(LauncherRuntimeOptions).Assembly),
    utc = DateTime.UtcNow
}));

app.MapGet("/launcher/profiles", async (
    ILauncherBootstrapService launcherBootstrapService,
    CancellationToken cancellationToken) =>
{
    IReadOnlyList<LauncherClientProfileSnapshot> profiles = await launcherBootstrapService.GetProfilesAsync(cancellationToken);
    return Results.Ok(profiles);
});

app.MapGet("/launcher/bootstrap/{profileKey}", async (
    HttpContext httpContext,
    string profileKey,
    string? deviceKind,
    ILauncherBootstrapService launcherBootstrapService,
    CancellationToken cancellationToken) =>
{
    string? sessionTicket =
        httpContext.Request.Headers[SessionTicketHeaderName].FirstOrDefault()
        ?? httpContext.Request.Query["ticket"].FirstOrDefault();
    LauncherBootstrapSnapshot? snapshot = await launcherBootstrapService.BuildAsync(
        profileKey,
        sessionTicket,
        httpContext.Request.Headers.UserAgent.FirstOrDefault(),
        TryParseDeviceKind(deviceKind),
        cancellationToken);
    return snapshot is null ? Results.NotFound() : Results.Ok(snapshot);
});

app.MapGet("/launcher/bootstrap", async (
    HttpContext httpContext,
    string? deviceKind,
    ILauncherBootstrapService launcherBootstrapService,
    CancellationToken cancellationToken) =>
{
    string? sessionTicket =
        httpContext.Request.Headers[SessionTicketHeaderName].FirstOrDefault()
        ?? httpContext.Request.Query["ticket"].FirstOrDefault();
    LauncherBootstrapSnapshot? snapshot = await launcherBootstrapService.BuildAsync(
        string.Empty,
        sessionTicket,
        httpContext.Request.Headers.UserAgent.FirstOrDefault(),
        TryParseDeviceKind(deviceKind),
        cancellationToken);
    return snapshot is null ? Results.NotFound() : Results.Ok(snapshot);
});

app.MapGet("/launcher/session/current", async (
    string? ticket,
    ILauncherBootstrapService launcherBootstrapService,
    IHttpClientFactory httpClientFactory,
    Microsoft.Extensions.Options.IOptions<LauncherRuntimeOptions> launcherOptions,
    CancellationToken cancellationToken) =>
{
    LauncherBootstrapSnapshot? bootstrap = await launcherBootstrapService.BuildAsync(
        string.Empty,
        ticket,
        null,
        null,
        cancellationToken);

    if (bootstrap?.Session is null)
    {
        return Results.Unauthorized();
    }

    return await ProxyGatewayGetAsync(
        httpClientFactory,
        launcherOptions.Value,
        $"/hotel/sessions/{bootstrap.Session.CharacterId}",
        ticket,
        cancellationToken);
});

app.MapPost("/launcher/telemetry", (
    LauncherTelemetryInput input,
    LauncherTelemetryStore telemetryStore) =>
{
    if (string.IsNullOrWhiteSpace(input.Ticket) || string.IsNullOrWhiteSpace(input.EventKey))
    {
        return Results.BadRequest(new { error = "ticket_and_event_required" });
    }

    telemetryStore.Append(new LauncherTelemetryEvent(
        Ticket: input.Ticket.Trim(),
        EventKey: input.EventKey.Trim(),
        Detail: string.IsNullOrWhiteSpace(input.Detail) ? null : input.Detail.Trim(),
        OccurredAtUtc: DateTime.UtcNow,
        Data: input.Data ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)));

    return Results.Ok(new { succeeded = true });
});

app.MapGet("/launcher/telemetry/current", (
    string? ticket,
    LauncherTelemetryStore telemetryStore) =>
{
    if (string.IsNullOrWhiteSpace(ticket))
    {
        return Results.BadRequest(new { error = "ticket_required" });
    }

    return Results.Ok(telemetryStore.GetByTicket(ticket.Trim()));
});

app.MapGet("/launcher/access-codes/current", async (
    string? ticket,
    ILauncherBootstrapService launcherBootstrapService,
    LauncherAccessCodeStore accessCodeStore,
    CancellationToken cancellationToken) =>
{
    LauncherBootstrapSnapshot? bootstrap = await launcherBootstrapService.BuildAsync(
        string.Empty,
        ticket,
        null,
        null,
        cancellationToken);

    if (bootstrap?.Session is null)
    {
        return Results.Unauthorized();
    }

    LauncherAccessCodeSnapshot? snapshot = accessCodeStore.GetCurrentByTicket(bootstrap.Session.Ticket);
    return snapshot is null
        ? Results.NotFound(new { error = "access_code_not_found" })
        : Results.Ok(new
        {
            code = snapshot.Code,
            expiresAtUtc = snapshot.ExpiresAtUtc,
            issuedAtUtc = snapshot.IssuedAtUtc,
            platformKind = snapshot.PlatformKind
        });
});

app.MapPost("/launcher/access-codes", async (
    LauncherAccessCodeIssueInput input,
    ILauncherBootstrapService launcherBootstrapService,
    LauncherAccessCodeStore accessCodeStore,
    LauncherTelemetryStore telemetryStore,
    CancellationToken cancellationToken) =>
{
    LauncherBootstrapSnapshot? bootstrap = await launcherBootstrapService.BuildAsync(
        string.Empty,
        input.Ticket,
        null,
        null,
        cancellationToken);

    if (bootstrap?.Session is null)
    {
        return Results.Unauthorized();
    }

    if (bootstrap.LaunchEntitlement is not null && !bootstrap.LaunchEntitlement.CanLaunch)
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    LauncherAccessCodeSnapshot snapshot = accessCodeStore.Issue(bootstrap.Session.Ticket, input.PlatformKind);
    telemetryStore.Append(new LauncherTelemetryEvent(
        Ticket: bootstrap.Session.Ticket,
        EventKey: "launcher_code_issued",
        Detail: "La CMS emitió un código único para abrir la app o launcher nativo.",
        OccurredAtUtc: DateTime.UtcNow,
        Data: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["code"] = snapshot.Code,
            ["platformKind"] = snapshot.PlatformKind ?? "unknown"
        }));

    return Results.Ok(new
    {
        code = snapshot.Code,
        issuedAtUtc = snapshot.IssuedAtUtc,
        expiresAtUtc = snapshot.ExpiresAtUtc,
        platformKind = snapshot.PlatformKind,
        launcherUrl = $"/launcher/loader?ticket={Uri.EscapeDataString(bootstrap.Session.Ticket)}"
    });
});

app.MapPost("/launcher/access-codes/redeem", async (
    LauncherAccessCodeRedeemInput input,
    ILauncherBootstrapService launcherBootstrapService,
    LauncherAccessCodeStore accessCodeStore,
    LauncherTelemetryStore telemetryStore,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(input.Code))
    {
        return Results.BadRequest(new { error = "code_required" });
    }

    LauncherAccessCodeSnapshot? redeemed = accessCodeStore.Redeem(input.Code.Trim());
    if (redeemed is null)
    {
        return Results.NotFound(new { error = "code_invalid_or_expired" });
    }

    LauncherBootstrapSnapshot? bootstrap = await launcherBootstrapService.BuildAsync(
        string.Empty,
        redeemed.Ticket,
        null,
        null,
        cancellationToken);

    if (bootstrap?.Session is null)
    {
        return Results.Unauthorized();
    }

    telemetryStore.Append(new LauncherTelemetryEvent(
        Ticket: redeemed.Ticket,
        EventKey: "launcher_code_redeemed",
        Detail: "La app o launcher nativo canjeó el código único contra el emulador.",
        OccurredAtUtc: DateTime.UtcNow,
        Data: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["code"] = redeemed.Code,
            ["deviceLabel"] = string.IsNullOrWhiteSpace(input.DeviceLabel) ? "unknown" : input.DeviceLabel.Trim(),
            ["platformKind"] = string.IsNullOrWhiteSpace(input.PlatformKind) ? (redeemed.PlatformKind ?? "unknown") : input.PlatformKind.Trim()
        }));

    string entryAssetUrl = string.IsNullOrWhiteSpace(bootstrap.EntryAssetUrl)
        ? string.Empty
        : $"{bootstrap.EntryAssetUrl}{(bootstrap.EntryAssetUrl.Contains('?') ? '&' : '?')}ticket={Uri.EscapeDataString(redeemed.Ticket)}";

    return Results.Ok(new
    {
        succeeded = true,
        code = redeemed.Code,
        redeemedAtUtc = redeemed.RedeemedAtUtc,
        ticket = redeemed.Ticket,
        launcherUrl = $"/launcher/loader?ticket={Uri.EscapeDataString(redeemed.Ticket)}",
        entryAssetUrl,
        session = bootstrap.Session,
        profile = bootstrap.Profile
    });
});

app.MapGet("/launcher/desktop/config", (
    Microsoft.Extensions.Options.IOptions<LauncherRuntimeOptions> launcherOptions) =>
{
    LauncherDesktopOptions desktopOptions = launcherOptions.Value.DesktopLauncher;
    return Results.Ok(new
    {
        hotelBaseUrl = desktopOptions.HotelBaseUrl,
        launcherApiBaseUrl = desktopOptions.LauncherApiBaseUrl,
        defaultChannel = desktopOptions.DefaultChannel,
        defaultProfileKey = desktopOptions.DefaultProfileKey,
        localConfigDefaults = desktopOptions.LocalConfigDefaults,
        detectedPlatform = DetectDesktopPlatform()
    });
});

app.MapGet("/launcher/update/channels", (
    Microsoft.Extensions.Options.IOptions<LauncherRuntimeOptions> launcherOptions) =>
{
    LauncherDesktopOptions desktopOptions = launcherOptions.Value.DesktopLauncher;
    return Results.Ok(new
    {
        defaultChannel = desktopOptions.DefaultChannel,
        channels = desktopOptions.UpdateChannels
    });
});

app.MapGet("/launcher/update/channel/{channelKey}", (
    string channelKey,
    Microsoft.Extensions.Options.IOptions<LauncherRuntimeOptions> launcherOptions) =>
{
    LauncherDesktopOptions desktopOptions = launcherOptions.Value.DesktopLauncher;
    LauncherUpdateChannelOptions? channel = desktopOptions.UpdateChannels
        .FirstOrDefault(candidate => string.Equals(candidate.ChannelKey, channelKey, StringComparison.OrdinalIgnoreCase));
    return channel is null ? Results.NotFound(new { error = "channel_not_found" }) : Results.Ok(channel);
});

app.MapGet("/launcher/launch-profiles", (
    string? platformKind,
    string? channel,
    Microsoft.Extensions.Options.IOptions<LauncherRuntimeOptions> launcherOptions) =>
{
    LauncherDesktopOptions desktopOptions = launcherOptions.Value.DesktopLauncher;
    string resolvedPlatform = ResolveDesktopPlatform(platformKind);
    IReadOnlyList<LauncherDesktopProfileOptions> profiles = desktopOptions.LaunchProfiles
        .Where(profile => string.IsNullOrWhiteSpace(channel)
            || string.Equals(profile.Channel, channel, StringComparison.OrdinalIgnoreCase))
        .Where(profile => profile.Platforms.Count == 0
            || profile.Platforms.Any(candidate => string.Equals(candidate, resolvedPlatform, StringComparison.OrdinalIgnoreCase)))
        .ToArray();

    return Results.Ok(new
    {
        platformKind = resolvedPlatform,
        defaultProfileKey = desktopOptions.DefaultProfileKey,
        profiles
    });
});

app.MapPost("/launcher/launch-profiles/select", async (
    LauncherProfileSelectionInput input,
    ILauncherBootstrapService launcherBootstrapService,
    LauncherTelemetryStore telemetryStore,
    Microsoft.Extensions.Options.IOptions<LauncherRuntimeOptions> launcherOptions,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(input.Ticket))
    {
        return Results.BadRequest(new { error = "ticket_required" });
    }

    if (string.IsNullOrWhiteSpace(input.ProfileKey))
    {
        return Results.BadRequest(new { error = "profile_key_required" });
    }

    LauncherBootstrapSnapshot? bootstrap = await launcherBootstrapService.BuildAsync(
        string.Empty,
        input.Ticket,
        null,
        null,
        cancellationToken);

    if (bootstrap?.Session is null)
    {
        return Results.Unauthorized();
    }

    LauncherDesktopOptions desktopOptions = launcherOptions.Value.DesktopLauncher;
    string resolvedPlatform = ResolveDesktopPlatform(input.PlatformKind);
    LauncherDesktopProfileOptions? profile = desktopOptions.LaunchProfiles.FirstOrDefault(candidate =>
        string.Equals(candidate.ProfileKey, input.ProfileKey, StringComparison.OrdinalIgnoreCase)
        && (candidate.Platforms.Count == 0
            || candidate.Platforms.Any(platform => string.Equals(platform, resolvedPlatform, StringComparison.OrdinalIgnoreCase))));

    if (profile is null)
    {
        return Results.NotFound(new { error = "profile_not_found" });
    }

    bool canLaunchFromDesktopApp =
        string.Equals(profile.ClientKind, "loader", StringComparison.OrdinalIgnoreCase)
        || string.Equals(profile.ClientKind, "web", StringComparison.OrdinalIgnoreCase);

    string launchUrl = string.Empty;
    if (canLaunchFromDesktopApp
        && !string.IsNullOrWhiteSpace(bootstrap.EntryAssetUrl))
    {
        launchUrl = $"{bootstrap.EntryAssetUrl}{(bootstrap.EntryAssetUrl.Contains('?') ? '&' : '?')}ticket={Uri.EscapeDataString(bootstrap.Session.Ticket)}";
    }

    string launchStrategy = string.Equals(profile.ClientKind, "loader", StringComparison.OrdinalIgnoreCase)
        ? "app_loader"
        : string.Equals(profile.ClientKind, "web", StringComparison.OrdinalIgnoreCase)
            ? "web_url"
            : "native_package";

    telemetryStore.Append(new LauncherTelemetryEvent(
        Ticket: bootstrap.Session.Ticket,
        EventKey: "launcher_profile_selected",
        Detail: "El launcher desktop seleccionó un modo de inicio.",
        OccurredAtUtc: DateTime.UtcNow,
        Data: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["profileKey"] = profile.ProfileKey,
            ["clientKind"] = profile.ClientKind,
            ["platformKind"] = resolvedPlatform,
            ["channel"] = profile.Channel
        }));

    return Results.Ok(new
    {
        succeeded = true,
        platformKind = resolvedPlatform,
        profile = profile,
        launchStrategy,
        canStartNow = !string.IsNullOrWhiteSpace(launchUrl),
        blockingReason = string.IsNullOrWhiteSpace(launchUrl) ? "native_client_not_published_yet" : null,
        launchUrl,
        arguments = ResolveLaunchArguments(profile, desktopOptions, bootstrap),
        entryExecutable = profile.EntryExecutable
    });
});

app.MapPost("/launcher/client-started", async (
    LauncherClientStartedInput input,
    ILauncherBootstrapService launcherBootstrapService,
    LauncherTelemetryStore telemetryStore,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(input.Ticket))
    {
        return Results.BadRequest(new { error = "ticket_required" });
    }

    LauncherBootstrapSnapshot? bootstrap = await launcherBootstrapService.BuildAsync(
        string.Empty,
        input.Ticket,
        null,
        null,
        cancellationToken);

    if (bootstrap?.Session is null)
    {
        return Results.Unauthorized();
    }

    telemetryStore.Append(new LauncherTelemetryEvent(
        Ticket: bootstrap.Session.Ticket,
        EventKey: "launcher_client_started",
        Detail: "El launcher desktop ejecutó el loader del juego.",
        OccurredAtUtc: DateTime.UtcNow,
        Data: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["profileKey"] = string.IsNullOrWhiteSpace(input.ProfileKey) ? "unknown" : input.ProfileKey.Trim(),
            ["clientKind"] = string.IsNullOrWhiteSpace(input.ClientKind) ? "unknown" : input.ClientKind.Trim(),
            ["platformKind"] = ResolveDesktopPlatform(input.PlatformKind)
        }));

    return Results.Ok(new
    {
        succeeded = true,
        recordedAtUtc = DateTime.UtcNow
    });
});

app.MapGet("/launcher/connection", async (
    string? ticket,
    IHttpClientFactory httpClientFactory,
    Microsoft.Extensions.Options.IOptions<LauncherRuntimeOptions> launcherOptions,
    CancellationToken cancellationToken) =>
{
    return await ProxyGatewayGetAsync(
        httpClientFactory,
        launcherOptions.Value,
        "/hotel/connection",
        ticket,
        cancellationToken);
});

app.MapGet("/launcher/connection-state", async (
    string? ticket,
    ILauncherBootstrapService launcherBootstrapService,
    IHttpClientFactory httpClientFactory,
    Microsoft.Extensions.Options.IOptions<LauncherRuntimeOptions> launcherOptions,
    LauncherTelemetryStore telemetryStore,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(ticket))
    {
        return Results.BadRequest(new { error = "ticket_required" });
    }

    LauncherBootstrapSnapshot? bootstrap = await launcherBootstrapService.BuildAsync(
        string.Empty,
        ticket,
        null,
        null,
        cancellationToken);

    if (bootstrap?.Session is null)
    {
        return Results.Unauthorized();
    }

    long? currentRoomId = await ResolveGatewayCurrentRoomIdAsync(
        httpClientFactory,
        launcherOptions.Value,
        bootstrap.Session.Ticket,
        cancellationToken);

    IReadOnlyList<LauncherTelemetryEvent> telemetryEvents = telemetryStore.GetByTicket(bootstrap.Session.Ticket);
    bool launchPermitted = bootstrap.LaunchEntitlement?.CanLaunch != false;
    bool codeIssued = HasTelemetryEvent(telemetryEvents, "launcher_code_issued");
    bool codeRedeemed = HasTelemetryEvent(telemetryEvents, "launcher_code_redeemed");
    bool clientStarted = HasTelemetryEvent(telemetryEvents, "launcher_client_started");
    bool presenceConfirmed = currentRoomId is not null;
    string phaseKey = ResolveLauncherConnectionPhaseKey(
        launchPermitted,
        codeIssued,
        codeRedeemed,
        clientStarted,
        presenceConfirmed);

    LauncherTelemetryEvent? lastEvent = telemetryEvents.LastOrDefault();

    return Results.Ok(new
    {
        sessionValid = true,
        launchPermitted,
        codeIssued,
        codeRedeemed,
        clientStarted,
        presenceConfirmed,
        currentRoomId,
        phaseKey,
        phaseDisplayName = ResolveLauncherConnectionPhaseDisplayName(phaseKey),
        lastEventKey = lastEvent?.EventKey,
        lastEventAtUtc = lastEvent?.OccurredAtUtc,
        observedAtUtc = DateTime.UtcNow
    });
});

app.MapGet("/launcher/runtime/room/{roomId:long}", async (
    long roomId,
    string? ticket,
    IHttpClientFactory httpClientFactory,
    Microsoft.Extensions.Options.IOptions<LauncherRuntimeOptions> launcherOptions,
    CancellationToken cancellationToken) =>
{
    return await ProxyGatewayGetAsync(
        httpClientFactory,
        launcherOptions.Value,
        $"/hotel/rooms/{roomId}/runtime",
        ticket,
        cancellationToken);
});

app.MapPost("/launcher/runtime/room-entry", async (
    HttpContext httpContext,
    RoomEntryProxyInput input,
    IHttpClientFactory httpClientFactory,
    Microsoft.Extensions.Options.IOptions<LauncherRuntimeOptions> launcherOptions,
    CancellationToken cancellationToken) =>
{
    return await ProxyGatewayPostAsync(
        httpClientFactory,
        launcherOptions.Value,
        "/hotel/rooms/entry",
        input.Ticket,
        new
        {
            roomId = input.RoomId,
            password = input.Password,
            spectatorMode = input.SpectatorMode
        },
        cancellationToken);
});

app.MapPost("/launcher/runtime/room-move", async (
    RoomMoveProxyInput input,
    IHttpClientFactory httpClientFactory,
    Microsoft.Extensions.Options.IOptions<LauncherRuntimeOptions> launcherOptions,
    CancellationToken cancellationToken) =>
{
    return await ProxyGatewayPostAsync(
        httpClientFactory,
        launcherOptions.Value,
        "/hotel/rooms/move",
        input.Ticket,
        new
        {
            roomId = input.RoomId,
            destinationX = input.DestinationX,
            destinationY = input.DestinationY
        },
        cancellationToken);
});

app.MapPost("/launcher/runtime/room-chat", async (
    RoomChatProxyInput input,
    IHttpClientFactory httpClientFactory,
    Microsoft.Extensions.Options.IOptions<LauncherRuntimeOptions> launcherOptions,
    CancellationToken cancellationToken) =>
{
    return await ProxyGatewayPostAsync(
        httpClientFactory,
        launcherOptions.Value,
        "/hotel/rooms/chat",
        input.Ticket,
        new
        {
            roomId = input.RoomId,
            message = input.Message
        },
        cancellationToken);
});

app.Run();

static string ResolveInformationalVersion(Assembly assembly)
{
    return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? assembly.GetName().Version?.ToString()
        ?? "unknown";
}

static LauncherDeviceKind? TryParseDeviceKind(string? rawValue)
{
    if (string.IsNullOrWhiteSpace(rawValue))
    {
        return null;
    }

    return Enum.TryParse<LauncherDeviceKind>(rawValue, true, out LauncherDeviceKind deviceKind)
        ? deviceKind
        : null;
}

static string DetectDesktopPlatform()
{
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        return "Windows";
    }

    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
    {
        return "macOS";
    }

    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    {
        return "Linux";
    }

    return "Unknown";
}

static string ResolveDesktopPlatform(string? rawPlatform)
{
    if (string.IsNullOrWhiteSpace(rawPlatform))
    {
        return DetectDesktopPlatform();
    }

    return rawPlatform.Trim() switch
    {
        "macos" or "MacOS" or "macOS" or "darwin" => "macOS",
        "windows" or "win32" or "Windows" => "Windows",
        "linux" or "Linux" => "Linux",
        "iphone" or "iOS" or "ios" => "iPhone",
        "android" or "Android" => "Android",
        var value => value
    };
}

static bool HasTelemetryEvent(IReadOnlyList<LauncherTelemetryEvent> events, string eventKey)
{
    return events.Any(candidate => string.Equals(candidate.EventKey, eventKey, StringComparison.OrdinalIgnoreCase));
}

static string ResolveLauncherConnectionPhaseKey(
    bool launchPermitted,
    bool codeIssued,
    bool codeRedeemed,
    bool clientStarted,
    bool presenceConfirmed)
{
    if (!launchPermitted)
    {
        return "launch_blocked";
    }

    if (presenceConfirmed)
    {
        return "presence_confirmed";
    }

    if (clientStarted)
    {
        return "client_started";
    }

    if (codeRedeemed)
    {
        return "code_redeemed";
    }

    if (codeIssued)
    {
        return "code_issued";
    }

    return "cms_authenticated";
}

static string ResolveLauncherConnectionPhaseDisplayName(string phaseKey)
{
    return phaseKey switch
    {
        "launch_blocked" => "Acceso bloqueado",
        "presence_confirmed" => "Dentro del hotel",
        "client_started" => "Cliente abierto",
        "code_redeemed" => "Código canjeado",
        "code_issued" => "Código emitido",
        _ => "Sesión web activa"
    };
}

static IReadOnlyList<string> ResolveLaunchArguments(
    LauncherDesktopProfileOptions profile,
    LauncherDesktopOptions desktopOptions,
    LauncherBootstrapSnapshot bootstrap)
{
    string entryAssetUrl = string.IsNullOrWhiteSpace(bootstrap.EntryAssetUrl)
        ? string.Empty
        : $"{bootstrap.EntryAssetUrl}{(bootstrap.EntryAssetUrl.Contains('?') ? '&' : '?')}ticket={Uri.EscapeDataString(bootstrap.Session?.Ticket ?? string.Empty)}";

    return profile.Arguments
        .Select(argument => argument
            .Replace("{ticket}", bootstrap.Session?.Ticket ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{entryAssetUrl}", entryAssetUrl, StringComparison.OrdinalIgnoreCase)
            .Replace("{launcherApiBaseUrl}", desktopOptions.LauncherApiBaseUrl, StringComparison.OrdinalIgnoreCase)
            .Replace("{hotelBaseUrl}", desktopOptions.HotelBaseUrl, StringComparison.OrdinalIgnoreCase))
        .ToArray();
}

static async Task<long?> ResolveGatewayCurrentRoomIdAsync(
    IHttpClientFactory httpClientFactory,
    LauncherRuntimeOptions launcherOptions,
    string sessionTicket,
    CancellationToken cancellationToken)
{
    using HttpClient httpClient = httpClientFactory.CreateClient();
    httpClient.BaseAddress = new Uri(launcherOptions.GatewayBaseUrl, UriKind.Absolute);
    httpClient.DefaultRequestHeaders.TryAddWithoutValidation(SessionTicketHeaderName, sessionTicket.Trim());

    HttpResponseMessage response = await httpClient.GetAsync("/hotel/connection", cancellationToken);
    if (!response.IsSuccessStatusCode)
    {
        return null;
    }

    await using Stream payload = await response.Content.ReadAsStreamAsync(cancellationToken);
    using JsonDocument document = await JsonDocument.ParseAsync(payload, cancellationToken: cancellationToken);
    if (!document.RootElement.TryGetProperty("currentRoomId", out JsonElement currentRoomElement))
    {
        return null;
    }

    if (currentRoomElement.ValueKind != JsonValueKind.Number)
    {
        return null;
    }

    return currentRoomElement.TryGetInt64(out long roomId) ? roomId : null;
}

static async Task<IResult> ProxyGatewayGetAsync(
    IHttpClientFactory httpClientFactory,
    LauncherRuntimeOptions launcherOptions,
    string path,
    string? sessionTicket,
    CancellationToken cancellationToken)
{
    using HttpClient httpClient = httpClientFactory.CreateClient();
    httpClient.BaseAddress = new Uri(launcherOptions.GatewayBaseUrl, UriKind.Absolute);
    if (!string.IsNullOrWhiteSpace(sessionTicket))
    {
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation(SessionTicketHeaderName, sessionTicket.Trim());
    }

    HttpResponseMessage response = await httpClient.GetAsync(path, cancellationToken);
    string body = await response.Content.ReadAsStringAsync(cancellationToken);
    string contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json; charset=utf-8";
    return Results.Content(body, contentType, Encoding.UTF8, (int)response.StatusCode);
}

static async Task<IResult> ProxyGatewayPostAsync(
    IHttpClientFactory httpClientFactory,
    LauncherRuntimeOptions launcherOptions,
    string path,
    string? sessionTicket,
    object payload,
    CancellationToken cancellationToken)
{
    using HttpClient httpClient = httpClientFactory.CreateClient();
    httpClient.BaseAddress = new Uri(launcherOptions.GatewayBaseUrl, UriKind.Absolute);
    if (!string.IsNullOrWhiteSpace(sessionTicket))
    {
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation(SessionTicketHeaderName, sessionTicket.Trim());
    }

    HttpResponseMessage response = await httpClient.PostAsJsonAsync(path, payload, cancellationToken);
    string body = await response.Content.ReadAsStringAsync(cancellationToken);
    string contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json; charset=utf-8";
    return Results.Content(body, contentType, Encoding.UTF8, (int)response.StatusCode);
}

static string RenderLauncherLoaderPage(string ticket, bool macLauncherReady)
{
    string macLauncherHref = macLauncherReady ? "/launcher/downloads/macos-arm64" : "#";

    return $$"""
<!DOCTYPE html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Launcher | Epsilon Hotel</title>
  <style>
    :root { color-scheme: dark; }
    * { box-sizing: border-box; }
    body {
      margin: 0;
      min-height: 100vh;
      font-family: "Trebuchet MS", Verdana, sans-serif;
      color: #eef4fb;
      background:
        linear-gradient(rgba(11, 39, 60, 0.18), rgba(11, 39, 60, 0.28)),
        linear-gradient(180deg, #2f83b0 0%, #1f6896 45%, #19557d 100%);
    }
    main {
      min-height: 100vh;
      display: grid;
      place-items: center;
      padding: 24px;
    }
    .launcher-modal {
      width: min(760px, 100%);
      background: linear-gradient(180deg, #0f5f8a 0%, #0d5a83 100%);
      border: 1px solid rgba(255,255,255,.16);
      border-radius: 24px;
      box-shadow: 0 26px 80px rgba(0,0,0,.3);
      overflow: hidden;
    }
    .launcher-head {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 16px;
      padding: 20px 24px;
      background: rgba(8, 34, 53, 0.32);
      border-bottom: 1px solid rgba(255,255,255,.08);
    }
    .launcher-head h1 {
      margin: 0;
      font-size: clamp(28px, 4vw, 44px);
      line-height: 1;
    }
    .launcher-head a {
      color: #ffffff;
      opacity: 0.88;
      text-decoration: none;
    }
    .launcher-body {
      display: grid;
      gap: 24px;
      padding: 26px 24px 28px;
    }
    .block {
      background: rgba(18, 44, 68, 0.26);
      border: 1px solid rgba(255,255,255,.08);
      border-radius: 20px;
      padding: 22px;
    }
    .eyebrow {
      margin: 0 0 10px;
      color: #cff3ff;
      text-transform: uppercase;
      letter-spacing: .12em;
      font-size: 12px;
      font-weight: 800;
    }
    .launcher-button {
      width: 100%;
      min-height: 84px;
      border-radius: 18px;
      border: 2px solid #8be15d;
      background: linear-gradient(180deg, #159742 0%, #0e8d3b 100%);
      color: #ffffff;
      font: inherit;
      font-size: 26px;
      font-weight: 700;
      cursor: pointer;
    }
    .launcher-button:disabled {
      opacity: .58;
      cursor: not-allowed;
    }
    .notice {
      display: none;
      padding: 16px 18px;
      border-radius: 16px;
      line-height: 1.4;
      background: rgba(143,209,255,.16);
      border: 1px solid rgba(143,209,255,.45);
    }
    .notice.error { background: rgba(214,87,87,.18); border-color: rgba(214,87,87,.55); display: block; }
    .notice.success { background: rgba(45,187,114,.18); border-color: rgba(45,187,114,.55); display: block; }
    .code-box {
      display: grid;
      gap: 18px;
      text-align: center;
    }
    .code-title {
      margin: 0;
      font-size: 18px;
      line-height: 1.45;
      color: #eef4fb;
    }
    .code-value-row {
      display: grid;
      grid-template-columns: 1fr auto;
      gap: 12px;
      align-items: center;
      padding: 18px 20px;
      border-radius: 16px;
      border: 1px solid rgba(255,255,255,.12);
      background: rgba(255,255,255,.06);
    }
    .code-value {
      font-size: 30px;
      letter-spacing: .14em;
      font-weight: 700;
      text-align: left;
    }
    .code-value.masked {
      letter-spacing: .08em;
    }
    .icon-button,
    .copy-button,
    .download-button {
      font: inherit;
      cursor: pointer;
      border: 0;
      border-radius: 14px;
      text-decoration: none;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      font-weight: 700;
    }
    .icon-button {
      width: 52px;
      height: 52px;
      background: rgba(255,255,255,.08);
      color: #ffffff;
    }
    .copy-button {
      min-height: 60px;
      padding: 12px 20px;
      background: linear-gradient(180deg, #1a9f48 0%, #11863b 100%);
      color: #ffffff;
    }
    .copy-button:disabled {
      opacity: .58;
      cursor: not-allowed;
    }
    .downloads {
      display: grid;
      gap: 14px;
    }
    .download-button {
      width: 100%;
      min-height: 68px;
      padding: 14px 18px;
      background: linear-gradient(180deg, #a48312 0%, #8d700f 100%);
      color: #ffffff;
      font-size: 22px;
    }
    .download-button.disabled {
      background: linear-gradient(180deg, #4b6679 0%, #354a59 100%);
      color: #d4e2ec;
      cursor: default;
      pointer-events: none;
    }
    .small-note {
      margin: 0;
      color: #d5e9f6;
      line-height: 1.45;
      text-align: center;
      font-size: 16px;
    }
    details {
      border-top: 1px solid rgba(255,255,255,.08);
      padding-top: 12px;
    }
    summary {
      cursor: pointer;
      font-weight: 800;
      color: #d8e6f2;
      list-style: none;
    }
    summary::-webkit-details-marker { display: none; }
    pre {
      margin: 12px 0 0;
      min-height: 60px;
      max-height: 220px;
      overflow: auto;
      white-space: pre-wrap;
      word-break: break-word;
      border-radius: 16px;
      padding: 14px;
      background: rgba(5,14,22,.55);
      border: 1px solid rgba(255,255,255,.1);
      font-family: ui-monospace, SFMono-Regular, Menlo, monospace;
      font-size: 13px;
    }
    @media (max-width: 640px) {
      main {
        padding: 16px;
      }
      .launcher-body,
      .launcher-head {
        padding-left: 16px;
        padding-right: 16px;
      }
      .launcher-head h1 {
        font-size: 24px;
      }
      .launcher-button,
      .download-button {
        font-size: 20px;
      }
      .code-value-row {
        grid-template-columns: 1fr;
      }
      .code-value {
        text-align: center;
        font-size: 24px;
      }
    }
  </style>
</head>
<body>
  <main>
    <section class="launcher-modal">
      <div class="launcher-head">
        <h1>Juega con la app de Epsilon</h1>
        <a href="http://127.0.0.1:4100/sites/epsilon-access/">Cerrar</a>
      </div>
      <div class="launcher-body">
        <button id="launch-button" class="launcher-button" type="button" disabled>Abrir launcher instalado</button>
        <div id="banner" class="notice"></div>

        <section class="block">
          <div class="code-box">
            <p class="code-title">Si tu app de Epsilon te pide un código de inicio, puedes copiarlo aquí.</p>
            <div class="code-value-row">
              <strong id="launcher-code-value" class="code-value masked">••••••••••••</strong>
              <button id="toggle-code-button" class="icon-button" type="button" aria-label="Mostrar código">👁</button>
            </div>
            <button id="copy-code-button" class="copy-button" type="button" disabled>Copiar código</button>
            <p id="launcher-copy" class="small-note">La app instalada ejecuta el loader del juego. El emulador es quien confirma la entrada real.</p>
          </div>
        </section>

        <section class="block">
          <p class="eyebrow">Descargas</p>
          <div class="downloads">
            <a class="download-button{{(macLauncherReady ? string.Empty : " disabled")}}" href="{{macLauncherHref}}">{{(macLauncherReady ? "Descargar para macOS" : "macOS próximamente")}}</a>
            <a class="download-button disabled" href="#">Descargar para Windows</a>
            <a class="download-button disabled" href="#">Descargar para Linux</a>
          </div>
          <p id="client-state-note" class="small-note">La web solo entrega el acceso. La app de Epsilon ejecuta el loader del juego.</p>
        </section>

        <section class="block">
          <details id="diagnostics-panel">
            <summary>Ver detalle técnico</summary>
            <pre id="bootstrap-output">loading...</pre>
          </details>
        </section>
      </div>
    </section>
  </main>
  <script>
    (function () {
      const params = new URLSearchParams(window.location.search);
      const ticket = params.get("ticket") || "";
      const banner = document.getElementById("banner");
      const launcherCopy = document.getElementById("launcher-copy");
      const bootstrapOutput = document.getElementById("bootstrap-output");
      const launchButton = document.getElementById("launch-button");
      const copyCodeButton = document.getElementById("copy-code-button");
      const toggleCodeButton = document.getElementById("toggle-code-button");
      const codeValue = document.getElementById("launcher-code-value");
      const clientStateNote = document.getElementById("client-state-note");
      const diagnosticsPanel = document.getElementById("diagnostics-panel");
      let currentCode = "";
      let codeVisible = false;

      function buildLauncherAppUrl() {
        if (!currentCode) {
          return "";
        }

        return "epsilonlauncher://redeem?code=" + encodeURIComponent(currentCode);
      }

      if (ticket && params.has("roomId")) {
        window.history.replaceState({}, "", "/launcher/loader?ticket=" + encodeURIComponent(ticket));
      }

      function setBanner(text, mode) {
        if (!text) {
          banner.style.display = "none";
          banner.textContent = "";
          banner.className = "notice";
          return;
        }

        banner.textContent = text;
        banner.className = "notice " + mode;
        banner.style.display = "block";
      }

      function renderCode() {
        if (!currentCode) {
          codeValue.textContent = "••••••••••••";
          codeValue.className = "code-value masked";
          return;
        }

        codeValue.textContent = codeVisible ? currentCode : "••••••••••••";
        codeValue.className = "code-value" + (codeVisible ? "" : " masked");
      }

      async function request(path, options) {
        const response = await fetch(path, options);
        const text = await response.text();
        let payload = null;

        try {
          payload = text ? JSON.parse(text) : null;
        } catch {
          payload = { raw: text };
        }

        if (!response.ok) {
          throw new Error(JSON.stringify(payload || { error: response.statusText }));
        }

        return payload;
      }

      async function resolveCode() {
        try {
          return await request("/launcher/access-codes/current?ticket=" + encodeURIComponent(ticket));
        } catch (error) {
          if (String(error).includes("access_code_not_found")) {
            return await request("/launcher/access-codes", {
              method: "POST",
              headers: { "content-type": "application/json" },
              body: JSON.stringify({ ticket: ticket, platformKind: "native_app" })
            });
          }

          throw error;
        }
      }

      async function refresh() {
        if (!ticket) {
          setBanner("Falta el ticket de sesión. Vuelve a la CMS y entra de nuevo.", "error");
          launcherCopy.textContent = "Sin ticket no existe acceso válido desde la CMS hacia la app.";
          clientStateNote.textContent = "La app y el emulador necesitan un ticket real o un código válido.";
          bootstrapOutput.textContent = JSON.stringify({ hasTicket: false }, null, 2);
          diagnosticsPanel.open = false;
          launchButton.disabled = true;
          copyCodeButton.disabled = true;
          currentCode = "";
          renderCode();
          return;
        }

        try {
          const bootstrap = await request("/launcher/bootstrap?ticket=" + encodeURIComponent(ticket));
          const canLaunch = Boolean(bootstrap.launchEntitlement && bootstrap.launchEntitlement.canLaunch);

          if (!canLaunch) {
            bootstrapOutput.textContent = JSON.stringify({
              hasTicket: true,
              canLaunch: false
            }, null, 2);
            diagnosticsPanel.open = false;
            launchButton.disabled = true;
            copyCodeButton.disabled = true;
            currentCode = "";
            renderCode();
          launcherCopy.textContent = "El acceso sigue bloqueado. La app todavía no puede ejecutar el loader.";
            clientStateNote.textContent = "Primero se necesita un acceso válido. Después la app ejecutará el loader del juego.";
            setBanner("Tu acceso todavía no puede abrir la app.", "error");
            return;
          }

          const code = await resolveCode();
          currentCode = code && code.code ? code.code : "";
          renderCode();
          launchButton.disabled = !currentCode;
          copyCodeButton.disabled = !currentCode;
          launcherCopy.textContent = "La app instalada ejecuta el loader del juego. El emulador confirma la entrada real.";
          clientStateNote.textContent = "Si ya tienes la app instalada, copia el código y úsalo allí. Si no la tienes, descárgala primero.";
          bootstrapOutput.textContent = JSON.stringify({
            hasTicket: true,
            canLaunch: true,
            hasCode: Boolean(currentCode),
            supportedDevices: bootstrap.profile && bootstrap.profile.supportedDevices ? bootstrap.profile.supportedDevices : []
          }, null, 2);
          diagnosticsPanel.open = false;
          setBanner("Tu acceso está listo. Usa la app de Epsilon para continuar.", "success");
        } catch (error) {
          launcherCopy.textContent = "La app no pudo resolver el acceso.";
          clientStateNote.textContent = "El launcher debe validar el acceso antes de ejecutar el loader.";
          bootstrapOutput.textContent = String(error);
          setBanner("El launcher no pudo validar el acceso o el código.", "error");
          diagnosticsPanel.open = true;
          launchButton.disabled = true;
          copyCodeButton.disabled = true;
          currentCode = "";
          renderCode();
        }
      }

      launchButton.addEventListener("click", async function () {
        if (!currentCode) {
          return;
        }

        try {
          const launcherUrl = buildLauncherAppUrl();
          if (launcherUrl) {
            window.location.href = launcherUrl;
          }

          launchButton.textContent = "Abriendo launcher…";
          setBanner("Intentando abrir la app de Epsilon. Si no se abre, copia el código manualmente.", "success");
          window.setTimeout(function () {
            launchButton.textContent = "Abrir launcher instalado";
          }, 1400);
        } catch {
          setBanner("No se pudo abrir la app. Puedes mostrar el código y copiarlo manualmente.", "error");
        }
      });

      copyCodeButton.addEventListener("click", async function () {
        if (!currentCode) {
          return;
        }

        try {
          await navigator.clipboard.writeText(currentCode);
          copyCodeButton.textContent = "Copiado";
          window.setTimeout(function () {
            copyCodeButton.textContent = "Copiar código";
          }, 1200);
        } catch {
          setBanner("No se pudo copiar el código.", "error");
        }
      });

      toggleCodeButton.addEventListener("click", function () {
        codeVisible = !codeVisible;
        renderCode();
      });

      refresh();
    })();
  </script>
</body>
</html>
""";
}

static string ResolveNativeLauncherMacDmgPath(string contentRootPath)
{
    string repoRoot = Path.GetFullPath(Path.Combine(contentRootPath, "..", ".."));
    return Path.Combine(repoRoot, "apps", "epsilon-launcher-native", "dist", "EpsilonLauncher-macOS-arm64.dmg");
}

#pragma warning disable CS8321
static string RenderLauncherPlayPage(string ticket, long roomId)
{
    string escapedTicket = System.Net.WebUtility.HtmlEncode(ticket);
    return $$"""
<!DOCTYPE html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Play | Epsilon Launcher</title>
  <style>
    body {
      margin: 0;
      font-family: "Trebuchet MS", Verdana, sans-serif;
      background: linear-gradient(180deg, #15334f 0%, #09131d 100%);
      color: #eef4fb;
      display: grid;
      place-items: center;
      min-height: 100vh;
      padding: 24px;
    }
    .card {
      width: min(560px, 100%);
      padding: 28px;
      border-radius: 24px;
      background: rgba(18,44,68,.92);
      border: 1px solid rgba(255,255,255,.12);
      box-shadow: 0 18px 50px rgba(0,0,0,.28);
    }
    h1 {
      margin: 0 0 12px;
      font-size: clamp(30px, 5vw, 42px);
      line-height: 1;
    }
    p {
      margin: 0 0 18px;
      color: #9fc0d8;
      line-height: 1.45;
    }
    a {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      min-height: 50px;
      padding: 12px 18px;
      border-radius: 14px;
      background: linear-gradient(180deg, #35cf7f, #1f9d5b);
      color: #062114;
      font-weight: 800;
      text-decoration: none;
    }
  </style>
</head>
<body>
  <article class="card">
    <h1>Vista interna del launcher</h1>
    <p>Esta superficie no es la entrada del usuario. El acceso real se prepara en el loader y continúa en la app.</p>
    <a href="/launcher/loader?ticket={{escapedTicket}}">Volver al loader</a>
  </article>
</body>
</html>
""";
}
#pragma warning restore CS8321

static void AddRootConfiguration(ConfigurationManager configuration, string environmentName, string applicationKey)
{
    string? repositoryRoot = TryFindRepositoryRoot(AppContext.BaseDirectory)
        ?? TryFindRepositoryRoot(Directory.GetCurrentDirectory());

    if (string.IsNullOrWhiteSpace(repositoryRoot))
    {
        return;
    }

    string configurationDirectory = Path.Combine(repositoryRoot, "configuration");
    if (!Directory.Exists(configurationDirectory))
    {
        return;
    }

    configuration
        .AddJsonFile(Path.Combine(configurationDirectory, "shared.json"), optional: true, reloadOnChange: true)
        .AddJsonFile(Path.Combine(configurationDirectory, $"shared.{environmentName}.json"), optional: true, reloadOnChange: true)
        .AddJsonFile(Path.Combine(configurationDirectory, $"{applicationKey}.json"), optional: true, reloadOnChange: true)
        .AddJsonFile(Path.Combine(configurationDirectory, $"{applicationKey}.{environmentName}.json"), optional: true, reloadOnChange: true)
        .AddJsonFile(Path.Combine(configurationDirectory, "features.json"), optional: true, reloadOnChange: true)
        .AddJsonFile(Path.Combine(configurationDirectory, $"features.{environmentName}.json"), optional: true, reloadOnChange: true);
}

static string? TryFindRepositoryRoot(string startPath)
{
    if (string.IsNullOrWhiteSpace(startPath))
    {
        return null;
    }

    DirectoryInfo? directory = new(startPath);
    if (!directory.Exists)
    {
        directory = directory.Parent;
    }

    while (directory is not null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "EpsilonEmulator.sln")) ||
            Directory.Exists(Path.Combine(directory.FullName, "configuration")))
        {
            return directory.FullName;
        }

        directory = directory.Parent;
    }

    return null;
}
