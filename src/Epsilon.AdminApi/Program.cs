using Epsilon.AdminApi;
using Epsilon.CoreGame;
using Epsilon.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
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
    // If no key is configured the endpoint is open (development convenience).
    if (string.IsNullOrWhiteSpace(options.AdminApiKey))
    {
        return true;
    }

    if (!context.Request.Headers.TryGetValue(AdminKeyHeaderName, out var values))
    {
        return false;
    }

    string? providedKey = values.FirstOrDefault();
    return string.Equals(providedKey, options.AdminApiKey, StringComparison.Ordinal);
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
