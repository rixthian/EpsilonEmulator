using Epsilon.Auth;
using Epsilon.CoreGame;
using Epsilon.Launcher;
using Epsilon.Persistence;
using Microsoft.Extensions.FileProviders;
using System.Reflection;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text;

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
    httpContext.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
    httpContext.Response.Headers.Pragma = "no-cache";
    return Results.Content(RenderLauncherLoaderPage(safeTicket), "text/html; charset=utf-8", Encoding.UTF8);
});

app.MapGet("/launcher/play", (HttpContext httpContext, string? ticket, long? roomId) =>
{
    string safeTicket = string.IsNullOrWhiteSpace(ticket) ? string.Empty : ticket.Trim();
    string location = $"/launcher/loader?ticket={Uri.EscapeDataString(safeTicket)}";
    httpContext.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
    httpContext.Response.Headers.Pragma = "no-cache";
    return Results.Redirect(location, permanent: false);
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

    string launchUrl = string.Empty;
    if (string.Equals(profile.ClientKind, "web", StringComparison.OrdinalIgnoreCase)
        && !string.IsNullOrWhiteSpace(bootstrap.EntryAssetUrl))
    {
        launchUrl = $"{bootstrap.EntryAssetUrl}{(bootstrap.EntryAssetUrl.Contains('?') ? '&' : '?')}ticket={Uri.EscapeDataString(bootstrap.Session.Ticket)}";
    }

    telemetryStore.Append(new LauncherTelemetryEvent(
        Ticket: bootstrap.Session.Ticket,
        EventKey: "launcher_profile_selected",
        Detail: "El launcher desktop seleccionó un perfil de arranque.",
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
        launchStrategy = string.Equals(profile.ClientKind, "web", StringComparison.OrdinalIgnoreCase) ? "web_url" : "native_package",
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
        Detail: "El launcher desktop abrió el cliente del hotel.",
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

static string RenderLauncherLoaderPage(string ticket)
{
    string escapedTicket = System.Net.WebUtility.HtmlEncode(ticket);
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
      font-family: "Trebuchet MS", Verdana, sans-serif;
      color: #eef4fb;
      background:
        radial-gradient(circle at top left, rgba(255, 200, 87, 0.18), transparent 25%),
        radial-gradient(circle at top right, rgba(64, 191, 255, 0.14), transparent 28%),
        linear-gradient(180deg, #17324d 0%, #0b1c2c 44%, #08131e 100%);
    }
    main {
      max-width: 1180px;
      margin: 0 auto;
      padding: 28px 20px 40px;
    }
    .shell { display: grid; gap: 20px; }
    .hero {
      display: grid;
      gap: 20px;
      grid-template-columns: minmax(0, 1.4fr) minmax(320px, .9fr);
      align-items: stretch;
    }
    .panel {
      background: rgba(18,44,68,.92);
      border: 1px solid rgba(255,255,255,.12);
      border-radius: 26px;
      box-shadow: 0 18px 50px rgba(0,0,0,.28);
      overflow: hidden;
    }
    .hero-copy {
      position: relative;
      padding: 28px;
      min-height: 360px;
      background:
        linear-gradient(160deg, rgba(255,255,255,.05), rgba(255,255,255,0) 35%),
        linear-gradient(135deg, rgba(255, 200, 87, 0.14), transparent 32%),
        linear-gradient(180deg, rgba(27, 61, 95, 0.98), rgba(9, 25, 39, 0.98));
    }
    .hero-copy::after {
      content: "";
      position: absolute;
      right: -40px;
      bottom: -40px;
      width: 260px;
      height: 260px;
      border-radius: 36px;
      background:
        linear-gradient(180deg, rgba(53, 207, 127, 0.26), rgba(31, 157, 91, 0.08));
      transform: rotate(18deg);
      opacity: .75;
    }
    .hero-art {
      position: relative;
      padding: 24px;
      background:
        linear-gradient(180deg, rgba(13, 34, 53, 0.96), rgba(7, 18, 28, 0.98));
    }
    .hero-art::before,
    .hero-art::after {
      content: "";
      position: absolute;
      border-radius: 24px;
      filter: blur(2px);
    }
    .hero-art::before {
      inset: 20px auto auto 20px;
      width: 120px;
      height: 120px;
      background: linear-gradient(180deg, rgba(255, 214, 77, .22), rgba(255, 214, 77, 0));
    }
    .hero-art::after {
      inset: auto 18px 18px auto;
      width: 180px;
      height: 180px;
      background: linear-gradient(180deg, rgba(64, 191, 255, .18), rgba(64, 191, 255, 0));
    }
    .art-grid {
      position: relative;
      z-index: 1;
      display: grid;
      gap: 14px;
      grid-template-columns: repeat(2, minmax(0, 1fr));
    }
    .art-tile {
      min-height: 120px;
      border-radius: 22px;
      border: 1px solid rgba(255,255,255,.08);
      background:
        linear-gradient(180deg, rgba(255,255,255,.06), rgba(255,255,255,.01)),
        linear-gradient(180deg, rgba(22, 54, 84, .9), rgba(8, 21, 33, .96));
      padding: 18px;
      display: grid;
      align-content: end;
      gap: 8px;
    }
    .art-tile strong {
      font-size: 20px;
      line-height: 1;
    }
    .art-tile span {
      color: #9fc0d8;
      font-size: 14px;
    }
    h1,h2,h3 { margin: 0 0 12px; }
    h1 {
      max-width: 11ch;
      font-size: clamp(42px, 6vw, 68px);
      line-height: .95;
    }
    h2 {
      font-size: clamp(28px, 3vw, 40px);
      line-height: .98;
    }
    h3 {
      font-size: 20px;
    }
    .eyebrow {
      margin: 0 0 8px;
      color: #ffd64d;
      text-transform: uppercase;
      letter-spacing: .14em;
      font-size: 12px;
      font-weight: 700;
    }
    .lede {
      max-width: 52ch;
      color: #9fc0d8;
      font-size: 18px;
      line-height: 1.45;
    }
    .sublede {
      color: #9fc0d8;
      font-size: 15px;
      line-height: 1.45;
      max-width: 42ch;
    }
    .hero-actions,
    .actions {
      display: flex;
      gap: 12px;
      flex-wrap: wrap;
    }
    .hero-actions { margin-top: 22px; }
    .chips {
      display: flex;
      gap: 10px;
      flex-wrap: wrap;
      margin-top: 18px;
    }
    .chip {
      padding: 8px 12px;
      border-radius: 999px;
      background: rgba(255,255,255,.08);
      border: 1px solid rgba(255,255,255,.1);
      color: #d8e6f2;
      font-size: 13px;
      font-weight: 700;
    }
    .grid {
      display: grid;
      gap: 20px;
      grid-template-columns: repeat(12, minmax(0, 1fr));
    }
    .span-4 { grid-column: span 4; }
    .span-5 { grid-column: span 5; }
    .span-6 { grid-column: span 6; }
    .span-7 { grid-column: span 7; }
    .span-12 { grid-column: span 12; }
    .stack { display: grid; gap: 12px; }
    .panel-body { padding: 24px; }
    .metric-grid {
      display: grid;
      gap: 14px;
      grid-template-columns: repeat(2, minmax(0, 1fr));
    }
    .runtime-grid {
      display: grid;
      gap: 14px;
      grid-template-columns: repeat(4, minmax(0, 1fr));
    }
    .metric-card {
      padding: 16px;
      border-radius: 18px;
      background: rgba(255,255,255,.04);
      border: 1px solid rgba(255,255,255,.08);
    }
    .metric-card span {
      display: block;
      color: #9fc0d8;
      font-size: 12px;
      text-transform: uppercase;
      letter-spacing: .08em;
      margin-bottom: 8px;
    }
    .metric-card strong {
      font-size: 22px;
      line-height: 1.05;
    }
    .notice {
      display: none;
      margin: 0 0 16px;
      padding: 14px 16px;
      border-radius: 16px;
      background: rgba(143,209,255,.16);
      border: 1px solid rgba(143,209,255,.45);
    }
    .notice.error { background: rgba(214,87,87,.18); border-color: rgba(214,87,87,.55); display:block; }
    .notice.success { background: rgba(45,187,114,.18); border-color: rgba(45,187,114,.55); display:block; }
    input, button { font: inherit; }
    input {
      width: 100%;
      padding: 14px 16px;
      border-radius: 14px;
      border: 1px solid rgba(255,255,255,.18);
      background: rgba(5,14,22,.45);
      color: #eef4fb;
    }
    button {
      min-height: 50px;
      padding: 12px 18px;
      border: 0;
      border-radius: 14px;
      cursor: pointer;
      background: linear-gradient(180deg, #35cf7f, #1f9d5b);
      color: #062114;
      font-weight: 800;
    }
    button.secondary { background: linear-gradient(180deg, #35536d, #24384c); color: #eef4fb; }
    button.ghost {
      background: transparent;
      border: 1px solid rgba(255,255,255,.14);
      color: #eef4fb;
    }
    button:disabled {
      opacity: .55;
      cursor: not-allowed;
    }
    pre {
      margin: 0;
      min-height: 80px;
      max-height: 320px;
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
    dl { margin: 0; display: grid; gap: 12px; }
    .summary-grid {
      grid-template-columns: repeat(3, minmax(0, 1fr));
    }
    dt { color: #9fc0d8; font-size: 12px; text-transform: uppercase; letter-spacing: .08em; }
    dd { margin: 4px 0 0; font-weight: 700; font-size: 17px; line-height: 1.2; }
    .status-pill {
      display: inline-flex;
      align-items: center;
      gap: 8px;
      padding: 10px 14px;
      border-radius: 999px;
      background: rgba(255,255,255,.08);
      border: 1px solid rgba(255,255,255,.12);
      font-weight: 700;
    }
    .status-dot {
      width: 10px;
      height: 10px;
      border-radius: 50%;
      background: #ffd64d;
      box-shadow: 0 0 12px rgba(255,214,77,.55);
    }
    .status-dot.live {
      background: #35cf7f;
      box-shadow: 0 0 12px rgba(53,207,127,.55);
    }
    .status-dot.error {
      background: #ff7777;
      box-shadow: 0 0 12px rgba(255,119,119,.55);
    }
    .launcher-copy {
      position: relative;
      z-index: 1;
      max-width: 560px;
    }
    .feature-list {
      display: grid;
      gap: 12px;
    }
    .runtime-stage {
      margin-top: 18px;
      padding: 20px;
      border-radius: 20px;
      background:
        linear-gradient(180deg, rgba(21,61,93,.72), rgba(6,18,29,.94)),
        linear-gradient(180deg, rgba(255,255,255,.05), rgba(255,255,255,0));
      border: 1px solid rgba(255,255,255,.08);
    }
    .feature-row {
      display: grid;
      gap: 8px;
      grid-template-columns: 1fr auto;
      align-items: center;
      padding: 14px 16px;
      border-radius: 16px;
      background: rgba(255,255,255,.04);
      border: 1px solid rgba(255,255,255,.08);
    }
    .feature-row strong {
      display: block;
      margin-bottom: 4px;
      font-size: 15px;
    }
    .feature-row span {
      color: #9fc0d8;
      font-size: 13px;
    }
    .mini-label {
      padding: 7px 10px;
      border-radius: 999px;
      font-size: 12px;
      font-weight: 800;
      background: rgba(53,207,127,.16);
      border: 1px solid rgba(53,207,127,.32);
      color: #c9f5db;
    }
    details {
      border-top: 1px solid rgba(255,255,255,.08);
      padding-top: 14px;
    }
    summary {
      cursor: pointer;
      font-weight: 800;
      color: #d8e6f2;
      margin-bottom: 12px;
      list-style: none;
    }
    summary::-webkit-details-marker { display: none; }
    a { color: #8fd1ff; }
    .footer-note {
      color: #9fc0d8;
      font-size: 14px;
      line-height: 1.45;
    }
    .hidden { display: none; }
    @media (max-width: 920px) {
      .hero { grid-template-columns: 1fr; }
      .grid { grid-template-columns: repeat(1, minmax(0, 1fr)); }
      .span-4, .span-5, .span-6, .span-7, .span-12 { grid-column: span 1; }
      .summary-grid, .metric-grid, .runtime-grid { grid-template-columns: 1fr; }
    }
  </style>
</head>
<body>
  <main>
    <div class="shell">
      <section class="hero">
        <article class="panel hero-copy">
          <div class="launcher-copy">
            <p class="eyebrow">Habbo Launcher</p>
            <h1>Abre la app y entra al hotel.</h1>
            <p class="lede">La CMS solo entrega el acceso. El usuario no está dentro del hotel hasta que el cliente y el emulador confirman la entrada real.</p>
            <div id="banner" class="notice"></div>
            <div class="hero-actions">
              <button id="launch-button" type="button" disabled>Abrir Habbo</button>
              <a class="ghost-link" href="http://127.0.0.1:4100/sites/epsilon-access/">Volver a la CMS</a>
            </div>
          </div>
        </article>
        <article class="panel hero-art">
          <div class="art-grid">
            <div class="art-tile">
              <strong>1</strong>
              <span>Preparar acceso</span>
            </div>
            <div class="art-tile">
              <strong>2</strong>
              <span>Abrir app o cliente</span>
            </div>
            <div class="art-tile">
              <strong>3</strong>
              <span>Conectar al emulador</span>
            </div>
            <div class="art-tile">
              <strong>4</strong>
              <span>Confirmar entrada real</span>
            </div>
          </div>
        </article>
      </section>

      <section class="grid">
        <article class="panel span-7">
          <div class="panel-body">
            <p class="eyebrow">Launcher</p>
            <h2>Estado del launcher</h2>
            <p class="footer-note" id="launcher-copy">El launcher está validando si puede entregar el control al cliente. Todavía no existe entrada confirmada al hotel.</p>
          </div>
        </article>

        <article class="panel span-5">
          <div class="panel-body">
            <p class="eyebrow">Secuencia</p>
            <h2>Cómo entra el usuario</h2>
            <div class="feature-list">
              <div class="feature-row">
                <div>
                  <strong>1. CMS</strong>
                  <span>Login y sesión web.</span>
                </div>
                <div class="mini-label">web</div>
              </div>
              <div class="feature-row">
                <div>
                  <strong>2. Launcher</strong>
                  <span>Abre el cliente o la app.</span>
                </div>
                <div class="mini-label" id="access-badge">handoff</div>
              </div>
              <div class="feature-row">
                <div>
                  <strong>3. Emulador</strong>
                  <span>Confirma la presencia real.</span>
                </div>
                <div class="mini-label" id="client-badge">runtime</div>
              </div>
            </div>
          </div>
        </article>

        <article class="panel span-12">
          <div class="panel-body">
            <p class="eyebrow">Cliente</p>
            <h2>Disponibilidad del cliente</h2>
            <div class="stack" style="margin-top: 18px;">
              <div class="feature-row">
                <div>
                  <strong id="client-state-title">Validando cliente</strong>
                  <span id="client-state-copy">El launcher está comprobando si existe un cliente publicado y listo para abrir.</span>
                </div>
                <div class="mini-label" id="client-state-badge">checking</div>
              </div>
              <p class="footer-note" id="client-state-note">Mientras no exista un cliente publicado, el launcher no puede afirmar que el usuario está dentro del hotel.</p>
            </div>
          </div>
        </article>

        <article class="panel span-12">
          <div class="panel-body">
            <div class="actions" style="justify-content: space-between; align-items: center; margin-bottom: 12px;">
              <div>
                <p class="eyebrow">Avanzado</p>
                <h3>Diagnóstico oculto</h3>
              </div>
              <div class="status-pill">
                <span id="status-dot" class="status-dot"></span>
                <span id="status-text">validando</span>
              </div>
            </div>
            <p class="footer-note">La información técnica queda escondida por defecto. Solo se abre cuando hace falta depurar el launcher.</p>
            <details id="diagnostics-panel">
              <summary>Ver detalle técnico</summary>
              <pre id="bootstrap-output">loading...</pre>
            </details>
          </div>
        </article>

      </section>
    </div>
  </main>
  <script>
    (function () {
      const params = new URLSearchParams(window.location.search);
      const ticket = params.get("ticket") || "";
      const banner = document.getElementById("banner");
      const launcherCopy = document.getElementById("launcher-copy");
      const bootstrapOutput = document.getElementById("bootstrap-output");
      const launchButton = document.getElementById("launch-button");
      const accessBadge = document.getElementById("access-badge");
      const clientBadge = document.getElementById("client-badge");
      const clientStateTitle = document.getElementById("client-state-title");
      const clientStateCopy = document.getElementById("client-state-copy");
      const clientStateBadge = document.getElementById("client-state-badge");
      const clientStateNote = document.getElementById("client-state-note");
      const statusDot = document.getElementById("status-dot");
      const statusText = document.getElementById("status-text");
      const diagnosticsPanel = document.getElementById("diagnostics-panel");
      let currentBootstrap = null;

      if (ticket && params.has("roomId")) {
        window.history.replaceState({}, "", "/launcher/loader?ticket=" + encodeURIComponent(ticket));
      }

      function setStatus(mode, text) {
        statusDot.className = "status-dot " + mode;
        statusText.textContent = text;
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

      async function request(path, options) {
        const response = await fetch(path, options);
        const text = await response.text();
        let payload = null;
        try { payload = text ? JSON.parse(text) : null; } catch { payload = { raw: text }; }
        if (!response.ok) {
          throw new Error(JSON.stringify(payload || { error: response.statusText }));
        }
        return payload;
      }

      async function refresh() {
        if (!ticket) {
          setBanner("Falta el ticket de sesión. Vuelve a la CMS y entra de nuevo.", "error");
          accessBadge.textContent = "error";
          clientBadge.textContent = "none";
          launcherCopy.textContent = "Sin handoff válido, el launcher no puede abrir ningún cliente.";
          clientStateTitle.textContent = "No hay ticket";
          clientStateCopy.textContent = "Sin ticket no existe handoff válido desde la CMS hacia el launcher.";
          clientStateBadge.textContent = "error";
          clientStateNote.textContent = "La entrada al hotel solo puede empezar desde un ticket real generado por el backend.";
          bootstrapOutput.textContent = JSON.stringify({ hasTicket: false }, null, 2);
          diagnosticsPanel.open = false;
          launchButton.disabled = true;
          launchButton.textContent = "Abrir Habbo";
          setStatus("error", "ticket requerido");
          return;
        }
        try {
          const bootstrap = await request("/launcher/bootstrap?ticket=" + encodeURIComponent(ticket));
          const canLaunch = Boolean(bootstrap.launchEntitlement && bootstrap.launchEntitlement.canLaunch);
          const entryAssetUrl = bootstrap.entryAssetUrl || "";
          const clientAvailable = await hasClientAsset(entryAssetUrl);
          accessBadge.textContent = canLaunch ? "ready" : "blocked";
          clientBadge.textContent = clientAvailable ? "ready" : "pending";
          launcherCopy.textContent = canLaunch
            ? "El launcher recibió acceso válido. Falta abrir el cliente para empezar la sesión real."
            : "El launcher recibió el handoff, pero el acceso todavía no permite abrir el cliente.";
          currentBootstrap = bootstrap;
          bootstrapOutput.textContent = JSON.stringify({
            hasSession: Boolean(bootstrap.session),
            canLaunch,
            clientAvailable,
            hasEntryAssetUrl: Boolean(bootstrap.entryAssetUrl),
            supportedDevices: bootstrap.profile && bootstrap.profile.supportedDevices ? bootstrap.profile.supportedDevices : []
          }, null, 2);
          diagnosticsPanel.open = false;

          if (!canLaunch) {
            launchButton.disabled = true;
            launchButton.textContent = "Acceso bloqueado";
            clientStateTitle.textContent = "Acceso bloqueado";
            clientStateCopy.textContent = "El launcher no puede abrir el cliente hasta que el acceso quede autorizado.";
            clientStateBadge.textContent = "blocked";
            clientStateNote.textContent = "Aunque exista handoff, eso no significa que el usuario haya entrado al hotel.";
            setBanner("El launcher recibió el handoff, pero el acceso al cliente está bloqueado.", "error");
            setStatus("error", "acceso bloqueado");
            return;
          }

          if (!clientAvailable) {
            launchButton.disabled = true;
            launchButton.textContent = "Cliente no publicado";
            clientStateTitle.textContent = "Cliente no disponible";
            clientStateCopy.textContent = "El launcher está listo, pero todavía no existe un cliente final publicado para abrir el hotel.";
            clientStateBadge.textContent = "pending";
            clientStateNote.textContent = "Hasta que el cliente no exista y no confirme runtime, no hay entrada real al hotel.";
            setBanner("El launcher está listo. Falta el cliente publicado.", "success");
            setStatus("live", "launcher listo");
            return;
          }

          launchButton.disabled = false;
          launchButton.textContent = "Abrir Habbo";
          clientStateTitle.textContent = "Cliente disponible";
          clientStateCopy.textContent = "El launcher ya puede entregar el control al cliente del hotel.";
          clientStateBadge.textContent = "ready";
          clientStateNote.textContent = "Abrir Habbo solo abre el cliente. La presencia real dentro del hotel sigue dependiendo del emulador.";
          setBanner("El launcher está listo para abrir el cliente.", "success");
          setStatus("live", "listo para abrir");
        } catch (error) {
          accessBadge.textContent = "error";
          clientBadge.textContent = "error";
          launcherCopy.textContent = "El launcher no pudo resolver el handoff.";
          clientStateTitle.textContent = "Error del loader";
          clientStateCopy.textContent = "No se pudo validar el bootstrap o el acceso.";
          clientStateBadge.textContent = "error";
          clientStateNote.textContent = "El launcher debe resolver este fallo antes de intentar abrir el cliente.";
          bootstrapOutput.textContent = String(error);
          setBanner("El loader no pudo validar bootstrap o conexión.", "error");
          diagnosticsPanel.open = true;
          launchButton.disabled = true;
          launchButton.textContent = "Abrir Habbo";
          setStatus("error", "error");
        }
      }

      async function hasClientAsset(entryAssetUrl) {
        if (!entryAssetUrl) {
          return false;
        }
        try {
          const response = await fetch(entryAssetUrl, { method: "HEAD" });
          return response.ok;
        } catch {
          return false;
        }
      }

      launchButton.addEventListener("click", async function () {
        const entryAssetUrl = currentBootstrap && currentBootstrap.entryAssetUrl ? currentBootstrap.entryAssetUrl : "";
        if (!entryAssetUrl) {
          return;
        }

        launchButton.disabled = true;
        launchButton.textContent = "Abriendo…";
        const separator = entryAssetUrl.includes("?") ? "&" : "?";
        window.location.href = entryAssetUrl + separator + "ticket=" + encodeURIComponent(ticket);
      });

      refresh();
    })();
  </script>
</body>
</html>
""";
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
    :root { color-scheme: dark; }
    * { box-sizing: border-box; }
    body { margin: 0; font-family: "Trebuchet MS", Verdana, sans-serif; color: #eef4fb; background: linear-gradient(180deg, #0b1c2c 0%, #09131d 100%); }
    main { max-width: 1240px; margin: 0 auto; padding: 20px; display: grid; gap: 20px; }
    .topbar, .panel { background: rgba(18,44,68,.92); border: 1px solid rgba(255,255,255,.1); border-radius: 22px; box-shadow: 0 18px 50px rgba(0,0,0,.28); }
    .topbar { padding: 18px 20px; display: flex; justify-content: space-between; gap: 14px; align-items: center; flex-wrap: wrap; }
    .hero { display: grid; gap: 20px; grid-template-columns: minmax(0, 1.5fr) minmax(320px, .8fr); }
    .viewport {
      min-height: 520px; padding: 24px; position: relative; overflow: hidden;
      background: radial-gradient(circle at top center, rgba(64,191,255,.15), transparent 28%), linear-gradient(180deg, rgba(17,45,70,.95), rgba(7,18,28,.98));
    }
    .viewport::after {
      content: "";
      position: absolute;
      inset: auto -80px -120px auto;
      width: 420px;
      height: 420px;
      transform: rotate(18deg);
      border-radius: 42px;
      background: linear-gradient(180deg, rgba(53,207,127,.22), rgba(53,207,127,.05));
    }
    .viewport h1, .panel h2 { margin: 0 0 12px; }
    .eyebrow { color: #ffd64d; text-transform: uppercase; letter-spacing: .14em; font-size: 12px; font-weight: 800; margin: 0 0 8px; }
    .lede { color: #9fc0d8; max-width: 54ch; line-height: 1.45; }
    .hud { display: flex; gap: 12px; flex-wrap: wrap; margin-top: 20px; }
    .pill { padding: 10px 14px; border-radius: 999px; border: 1px solid rgba(255,255,255,.12); background: rgba(255,255,255,.06); font-weight: 700; }
    .stage {
      position: relative; z-index: 1; margin-top: 28px; min-height: 280px; border-radius: 24px; border: 1px solid rgba(255,255,255,.08);
      background: linear-gradient(180deg, rgba(26,78,116,.65), rgba(8,28,44,.95)), linear-gradient(180deg, rgba(255,255,255,.04), rgba(255,255,255,0));
      padding: 22px; display: grid; align-content: space-between;
    }
    .stage-grid { display: grid; gap: 14px; grid-template-columns: repeat(3, minmax(0, 1fr)); }
    .tile { padding: 16px; border-radius: 18px; background: rgba(7,18,28,.28); border: 1px solid rgba(255,255,255,.08); }
    .tile span { display: block; color: #9fc0d8; font-size: 12px; text-transform: uppercase; letter-spacing: .08em; margin-bottom: 8px; }
    .tile strong { font-size: 22px; line-height: 1.1; }
    .panel { padding: 20px; }
    .stack { display: grid; gap: 12px; }
    .actions { display: flex; gap: 12px; flex-wrap: wrap; }
    a.button-link { display: inline-flex; align-items: center; justify-content: center; min-height: 48px; padding: 12px 16px; border-radius: 14px; font: inherit; font-weight: 800; text-decoration: none; color: #eef4fb; background: linear-gradient(180deg, #35536d, #24384c); }
    button { min-height: 48px; padding: 12px 16px; border-radius: 14px; border: 0; cursor: pointer; font: inherit; font-weight: 800; background: linear-gradient(180deg, #35cf7f, #1f9d5b); color: #062114; }
    button.secondary { background: linear-gradient(180deg, #35536d, #24384c); color: #eef4fb; }
    .notice { display:none; padding: 12px 14px; border-radius: 14px; background: rgba(143,209,255,.16); border: 1px solid rgba(143,209,255,.45); }
    .notice.error { display:block; background: rgba(214,87,87,.18); border-color: rgba(214,87,87,.55); }
    .notice.success { display:block; background: rgba(45,187,114,.18); border-color: rgba(45,187,114,.55); }
    pre { margin: 0; max-height: 320px; overflow: auto; white-space: pre-wrap; word-break: break-word; border-radius: 14px; padding: 14px; background: rgba(5,14,22,.55); border: 1px solid rgba(255,255,255,.08); font-family: ui-monospace, SFMono-Regular, Menlo, monospace; font-size: 13px; }
    details { border-top: 1px solid rgba(255,255,255,.08); padding-top: 14px; }
    summary { cursor: pointer; font-weight: 800; }
    @media (max-width: 920px) { .hero, .stage-grid { grid-template-columns: 1fr; } }
  </style>
</head>
<body>
  <main>
    <section class="topbar">
      <div>
        <p class="eyebrow">Launcher técnico</p>
        <strong>Vista interna del runtime</strong>
      </div>
      <div class="hud">
        <div class="pill">Room <span id="room-pill">{{roomId}}</span></div>
        <a class="button-link" href="/launcher/loader?ticket={{escapedTicket}}">Volver al loader</a>
      </div>
    </section>
    <section class="hero">
      <article class="panel viewport">
        <div>
          <p class="eyebrow">Diagnóstico</p>
          <h1>Esta no es la entrada del usuario.</h1>
          <p class="lede">La entrada correcta sigue en el loader del launcher. Esta vista queda solo para inspección técnica del runtime mientras el cliente web final no existe como asset real.</p>
          <div id="play-banner" class="notice"></div>
        </div>
        <div class="stage">
          <div class="stage-grid">
            <div class="tile"><span>Sala</span><strong id="play-room-label">{{roomId}}</strong></div>
            <div class="tile"><span>Actores</span><strong id="play-actors-label">checking</strong></div>
            <div class="tile"><span>Items</span><strong id="play-items-label">checking</strong></div>
          </div>
          <div class="actions">
            <button id="play-refresh">Actualizar sala</button>
            <button id="play-move" class="secondary">Mover a 14,7</button>
            <button id="play-chat" class="secondary">Decir hola</button>
          </div>
        </div>
      </article>
      <article class="panel">
        <p class="eyebrow">Snapshot</p>
        <h2>Estado de sala</h2>
        <div class="stack">
          <div class="tile"><span>Nombre</span><strong id="play-name-label">checking</strong></div>
          <div class="tile"><span>Layout</span><strong id="play-layout-label">checking</strong></div>
          <div class="tile"><span>Tu avatar</span><strong id="play-self-label">checking</strong></div>
        </div>
        <details style="margin-top: 18px;">
          <summary>Ver snapshot técnico</summary>
          <pre id="play-output">loading...</pre>
        </details>
      </article>
    </section>
  </main>
  <script>
    (function () {
      const params = new URLSearchParams(window.location.search);
      const ticket = params.get("ticket") || "";
      const roomId = Number(params.get("roomId") || "{{roomId}}");
      const banner = document.getElementById("play-banner");
      const output = document.getElementById("play-output");
      const actorsLabel = document.getElementById("play-actors-label");
      const itemsLabel = document.getElementById("play-items-label");
      const nameLabel = document.getElementById("play-name-label");
      const layoutLabel = document.getElementById("play-layout-label");
      const selfLabel = document.getElementById("play-self-label");

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

      async function request(path, options) {
        const response = await fetch(path, options);
        const text = await response.text();
        let payload = null;
        try { payload = text ? JSON.parse(text) : null; } catch { payload = { raw: text }; }
        if (!response.ok) {
          throw new Error(JSON.stringify(payload || { error: response.statusText }));
        }
        return payload;
      }

      async function refreshRoom() {
        try {
          const snapshot = await request("/launcher/runtime/room/" + encodeURIComponent(roomId) + "?ticket=" + encodeURIComponent(ticket));
          const actors = Array.isArray(snapshot.actors) ? snapshot.actors : [];
          const items = Array.isArray(snapshot.items) ? snapshot.items : [];
          actorsLabel.textContent = String(actors.length);
          itemsLabel.textContent = String(items.length);
          nameLabel.textContent = snapshot.room && snapshot.room.name ? snapshot.room.name : "unknown";
          layoutLabel.textContent = snapshot.layout && snapshot.layout.layoutCode ? snapshot.layout.layoutCode : "unknown";
          const self = actors.find(function (actor) { return actor.actorKind === 0; });
          selfLabel.textContent = self && self.username ? self.username : "none";
          output.textContent = JSON.stringify(snapshot, null, 2);
          setBanner("", "success");
        } catch (error) {
          output.textContent = String(error);
          setBanner("No se pudo cargar el runtime de sala.", "error");
        }
      }

      async function act(path, payload, successText) {
        try {
          await request(path, {
            method: "POST",
            headers: { "content-type": "application/json" },
            body: JSON.stringify(payload)
          });
          setBanner(successText, "success");
          await refreshRoom();
        } catch (error) {
          setBanner(String(error), "error");
        }
      }

      document.getElementById("play-refresh").addEventListener("click", function () {
        refreshRoom();
      });
      document.getElementById("play-move").addEventListener("click", function () {
        act("/launcher/runtime/room-move", { ticket: ticket, roomId: roomId, destinationX: 14, destinationY: 7 }, "Movimiento ejecutado.");
      });
      document.getElementById("play-chat").addEventListener("click", function () {
        act("/launcher/runtime/room-chat", { ticket: ticket, roomId: roomId, message: "hola, primera prueba operativa" }, "Mensaje enviado.");
      });

      refreshRoom();
    })();
  </script>
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
