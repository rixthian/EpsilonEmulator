using Epsilon.AdminApi;
using Epsilon.CoreGame;
using Epsilon.Gateway;
using Epsilon.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;

const string AdminKeyHeaderName = "X-Epsilon-Admin-Key";

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

AddRootConfiguration(builder.Configuration, builder.Environment.EnvironmentName, "admin");
builder.Configuration.AddEnvironmentVariables(prefix: "EPSILON_");

builder.Services.AddAdminRuntime(builder.Configuration);
builder.Services.AddCoreGameRuntime();
builder.Services.AddPersistenceRuntime(builder.Configuration);

var app = builder.Build();

// Health and readiness endpoints are intentionally unauthenticated so load
// balancers and orchestrators can probe them without credentials.
app.MapGet("/health", (IOptions<AdminRuntimeOptions> adminOptions) => Results.Ok(new
{
    service = adminOptions.Value.ServiceName,
    status = "ok",
    version = ResolveInformationalVersion(typeof(AdminRuntimeOptions).Assembly),
    utc = DateTime.UtcNow
}));

app.MapGet("/readiness", (IPersistenceReadinessChecker persistenceChecker) =>
{
    PersistenceReadinessReport report = persistenceChecker.Check();
    return report.IsReady ? Results.Ok(report) : Results.Problem(
        detail: string.Join(" ", report.Issues),
        statusCode: StatusCodes.Status503ServiceUnavailable,
        title: "Infrastructure is not ready.");
});

// All endpoints below require the admin API key.
app.MapGet("/housekeeping/characters/{characterId:long}", async (
    HttpContext httpContext,
    long characterId,
    [FromServices] IOptions<AdminRuntimeOptions> adminOptions,
    [FromServices] IHousekeepingSnapshotService housekeepingSnapshotService,
    CancellationToken cancellationToken) =>
{
    if (!IsAuthorized(httpContext, adminOptions.Value))
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    HousekeepingSnapshot? snapshot = await housekeepingSnapshotService.BuildAsync(new CharacterId(characterId), cancellationToken);
    return snapshot is null ? Results.NotFound() : Results.Ok(snapshot);
});

app.MapGet("/cms/players/public/{publicId}", async (
    HttpContext httpContext,
    string publicId,
    [FromServices] IOptions<AdminRuntimeOptions> adminOptions,
    [FromServices] ICharacterProfileRepository characterProfileRepository,
    [FromServices] IHotelReadService hotelReadService,
    CancellationToken cancellationToken) =>
{
    if (!IsAuthorized(httpContext, adminOptions.Value))
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    CharacterProfile? profile = await characterProfileRepository.GetByPublicIdAsync(publicId, cancellationToken);
    if (profile is null)
    {
        return Results.NotFound();
    }

    CharacterHotelSnapshot? snapshot = await hotelReadService.GetCharacterSnapshotAsync(profile.CharacterId, cancellationToken);
    return snapshot is null ? Results.NotFound() : Results.Ok(snapshot);
});

app.MapGet("/runtime/rooms", async (
    HttpContext httpContext,
    [FromServices] IOptions<AdminRuntimeOptions> adminOptions,
    [FromServices] IRoomRuntimeCoordinator roomRuntimeCoordinator,
    CancellationToken cancellationToken) =>
{
    if (!IsAuthorized(httpContext, adminOptions.Value))
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    IReadOnlyList<RoomRuntimeCoordinationSnapshot> snapshots =
        await roomRuntimeCoordinator.GetSnapshotsAsync(cancellationToken);
    return Results.Ok(snapshots);
});

app.MapGet("/runtime/rooms/{roomId:long}", async (
    HttpContext httpContext,
    long roomId,
    [FromServices] IOptions<AdminRuntimeOptions> adminOptions,
    [FromServices] IRoomRuntimeCoordinator roomRuntimeCoordinator,
    CancellationToken cancellationToken) =>
{
    if (!IsAuthorized(httpContext, adminOptions.Value))
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    RoomRuntimeCoordinationSnapshot? snapshot =
        await roomRuntimeCoordinator.GetSnapshotAsync(new RoomId(roomId), cancellationToken);
    return snapshot is null ? Results.NotFound() : Results.Ok(snapshot);
});

app.MapGet("/diagnostics/packetlog", (
    HttpContext httpContext,
    [FromServices] IOptions<AdminRuntimeOptions> adminOptions,
    [FromServices] IPacketLogger packetLogger,
    [FromQuery] int count = 200) =>
{
    if (!IsAuthorized(httpContext, adminOptions.Value))
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    IReadOnlyList<PacketLogEntry> entries = packetLogger.GetRecent(Math.Clamp(count, 1, 2000));
    return Results.Ok(new { count = entries.Count, entries });
});

app.MapGet("/diagnostics/summary", async (
    HttpContext httpContext,
    [FromServices] IOptions<AdminRuntimeOptions> adminOptions,
    [FromServices] IHttpClientFactory httpClientFactory,
    [FromServices] IPersistenceReadinessChecker persistenceChecker,
    CancellationToken cancellationToken) =>
{
    if (!IsAuthorized(httpContext, adminOptions.Value))
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    PersistenceReadinessReport readiness = persistenceChecker.Check();
    GatewayDiagnosticsSummary? gateway = null;
    string? gatewayError = null;
    object? launcher = null;
    string? launcherError = null;

    if (!string.IsNullOrWhiteSpace(adminOptions.Value.GatewayBaseUrl))
    {
        try
        {
            using HttpClient httpClient = httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(adminOptions.Value.GatewayBaseUrl, UriKind.Absolute);
            gateway = await httpClient.GetFromJsonAsync<GatewayDiagnosticsSummary>("/diagnostics/summary", cancellationToken);
        }
        catch (Exception exception)
        {
            gatewayError = exception.Message;
        }
    }
    else
    {
        gatewayError = "GatewayBaseUrl is not configured.";
    }

    if (!string.IsNullOrWhiteSpace(adminOptions.Value.LauncherBaseUrl))
    {
        try
        {
            using HttpClient httpClient = httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(adminOptions.Value.LauncherBaseUrl, UriKind.Absolute);
            launcher = await httpClient.GetFromJsonAsync<object>("/health", cancellationToken);
        }
        catch (Exception exception)
        {
            launcherError = exception.Message;
        }
    }
    else
    {
        launcherError = "LauncherBaseUrl is not configured.";
    }

    AdminDiagnosticsSummary summary = new(
        Admin: new AdminServiceStatus(adminOptions.Value.ServiceName, DateTime.UtcNow),
        Persistence: readiness,
        Gateway: gateway,
        GatewayError: gatewayError,
        Launcher: launcher,
        LauncherError: launcherError,
        Overall: new AdminOverallStatus(
            readiness.IsReady && gatewayError is null && launcherError is null,
            !readiness.IsReady || gatewayError is not null || launcherError is not null ? "degraded" : "healthy"));

    return Results.Ok(summary);
});

// Emergency endpoints — require admin key and affect the live hotel state.

app.MapPost("/emergency/lockdown", (
    HttpContext httpContext,
    [FromServices] IOptions<AdminRuntimeOptions> adminOptions,
    [FromServices] IHotelOperationalState hotelOperationalState,
    [FromBody] LockdownRequest request) =>
{
    if (!IsAuthorized(httpContext, adminOptions.Value))
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    if (request.Active)
    {
        hotelOperationalState.ActivateLockdown(request.Message);
    }
    else
    {
        hotelOperationalState.DeactivateLockdown();
    }

    return Results.Ok(new
    {
        active = hotelOperationalState.IsLockdownActive,
        message = hotelOperationalState.LockdownMessage
    });
});

app.MapGet("/emergency/lockdown", (
    HttpContext httpContext,
    [FromServices] IOptions<AdminRuntimeOptions> adminOptions,
    [FromServices] IHotelOperationalState hotelOperationalState) =>
{
    if (!IsAuthorized(httpContext, adminOptions.Value))
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    return Results.Ok(new
    {
        active = hotelOperationalState.IsLockdownActive,
        message = hotelOperationalState.LockdownMessage
    });
});

app.MapPost("/emergency/rooms/{roomId:long}/evict", async (
    HttpContext httpContext,
    long roomId,
    [FromServices] IOptions<AdminRuntimeOptions> adminOptions,
    [FromServices] IRoomRuntimeRepository roomRuntimeRepository,
    [FromServices] IRoomRuntimeCoordinator roomRuntimeCoordinator,
    CancellationToken cancellationToken) =>
{
    if (!IsAuthorized(httpContext, adminOptions.Value))
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    RoomId targetRoomId = new(roomId);
    int evicted = await roomRuntimeRepository.EvictAllPlayersFromRoomAsync(targetRoomId, cancellationToken);
    await roomRuntimeCoordinator.SignalMutationAsync(
        targetRoomId,
        RoomRuntimeMutationKind.ActorPresenceChanged,
        cancellationToken);

    return Results.Ok(new { roomId, evicted });
});

app.MapPost("/emergency/broadcast", async (
    HttpContext httpContext,
    [FromBody] BroadcastRequest request,
    [FromServices] IOptions<AdminRuntimeOptions> adminOptions,
    [FromServices] IRoomRuntimeRepository roomRuntimeRepository,
    [FromServices] IRoomRuntimeCoordinator roomRuntimeCoordinator,
    CancellationToken cancellationToken) =>
{
    if (!IsAuthorized(httpContext, adminOptions.Value))
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    if (string.IsNullOrWhiteSpace(request.Message))
    {
        return Results.BadRequest(new { error = "Message cannot be empty." });
    }

    IReadOnlyList<RoomId> activeRooms = await roomRuntimeRepository.GetAllActiveRoomIdsAsync(cancellationToken);
    foreach (RoomId roomId in activeRooms)
    {
        await roomRuntimeRepository.AppendChatMessageAsync(
            roomId,
            senderActorId: 0,
            senderName: "Hotel",
            message: request.Message,
            messageKind: RoomChatMessageKind.System,
            cancellationToken);
        await roomRuntimeCoordinator.SignalMutationAsync(
            roomId,
            RoomRuntimeMutationKind.ChatMessageAppended,
            cancellationToken);
    }

    return Results.Ok(new { roomsReached = activeRooms.Count, message = request.Message });
});

app.Run();

static bool IsAuthorized(HttpContext context, AdminRuntimeOptions options)
{
    if (string.IsNullOrWhiteSpace(options.AdminApiKey))
    {
        return options.AllowMissingAdminApiKeyForLocalDevelopment && IsLocalRequest(context);
    }

    if (!context.Request.Headers.TryGetValue(AdminKeyHeaderName, out var values))
    {
        return false;
    }

    string? providedKey = values.FirstOrDefault();
    return string.Equals(providedKey, options.AdminApiKey, StringComparison.Ordinal);
}

static bool IsLocalRequest(HttpContext context)
{
    IPAddress? remoteIp = context.Connection.RemoteIpAddress;
    if (remoteIp is not null && IPAddress.IsLoopback(remoteIp))
    {
        return true;
    }

    string host = context.Request.Host.Host;
    return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
           IPAddress.TryParse(host, out IPAddress? hostAddress) && IPAddress.IsLoopback(hostAddress);
}

static string ResolveInformationalVersion(Assembly assembly)
{
    return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? assembly.GetName().Version?.ToString()
        ?? "unknown";
}

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

// Request/response models
public sealed record LockdownRequest(bool Active, string? Message);
public sealed record BroadcastRequest(string Message);
