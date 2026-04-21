using Epsilon.Auth;
using Epsilon.CoreGame;
using Epsilon.Gateway;
using Epsilon.Persistence;
using Epsilon.Protocol;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);
// Mutable hotel actions are bound to a server-issued session ticket instead of
// trusting caller-supplied character ids in request bodies.
const string SessionTicketHeaderName = "X-Epsilon-Session-Ticket";

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables(prefix: "EPSILON_");

builder.Services.AddGatewayRuntime(builder.Configuration);
builder.Services.AddPersistenceRuntime(builder.Configuration);
builder.Services.AddAuthRuntime(builder.Configuration);
builder.Services.AddCoreGameRuntime();
builder.Services.AddProtocolServices(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.MapGet("/health", (
    PacketRegistry registry,
    ProtocolSelfCheckService protocolSelfCheckService,
    Microsoft.Extensions.Options.IOptions<GatewayRuntimeOptions> gatewayOptions) => Results.Ok(new
{
    service = "Epsilon.Gateway",
    status = protocolSelfCheckService.Run().IsHealthy ? "ok" : "degraded",
    hotelName = gatewayOptions.Value.HotelName,
    version = ResolveInformationalVersion(typeof(GatewayRuntimeOptions).Assembly),
    compatibility = registry.Family,
    incomingPacketCount = registry.Incoming.Count,
    outgoingPacketCount = registry.Outgoing.Count,
    utc = DateTime.UtcNow
}));

app.MapPost("/auth/development/login", async (
    AuthenticationRequest request,
    IAuthenticator authenticator,
    CancellationToken cancellationToken) =>
{
    AuthenticationResult result = await authenticator.AuthenticateAsync(request, cancellationToken);

    return result.Succeeded
        ? Results.Ok(result)
        : Results.BadRequest(result);
});

app.MapGet("/auth/development/sessions/{ticket}", async (
    string ticket,
    ISessionStore sessionStore,
    CancellationToken cancellationToken) =>
{
    SessionTicket? session = await sessionStore.FindByTicketAsync(ticket, cancellationToken);
    return session is null ? Results.NotFound() : Results.Ok(session);
});

app.MapGet("/readiness", (IPersistenceReadinessChecker persistenceChecker) =>
{
    PersistenceReadinessReport report = persistenceChecker.Check();
    return report.IsReady ? Results.Ok(report) : Results.Problem(
        detail: string.Join(" ", report.Issues),
        statusCode: StatusCodes.Status503ServiceUnavailable,
        title: "Infrastructure is not ready.");
});

app.MapGet("/hotel/characters/{characterId:long}", async (
    long characterId,
    IHotelReadService hotelReadService,
    CancellationToken cancellationToken) =>
{
    CharacterHotelSnapshot? snapshot = await hotelReadService.GetCharacterSnapshotAsync(new CharacterId(characterId), cancellationToken);
    return snapshot is null ? Results.NotFound() : Results.Ok(snapshot);
});

app.MapGet("/hotel/rooms/{roomId:long}", async (
    long roomId,
    IHotelReadService hotelReadService,
    CancellationToken cancellationToken) =>
{
    RoomHotelSnapshot? snapshot = await hotelReadService.GetRoomSnapshotAsync(new RoomId(roomId), cancellationToken);
    return snapshot is null ? Results.NotFound() : Results.Ok(snapshot);
});

app.MapGet("/hotel/bootstrap/{characterId:long}", async (
    long characterId,
    IHotelBootstrapService bootstrapService,
    CancellationToken cancellationToken) =>
{
    HotelBootstrapSnapshot? snapshot = await bootstrapService.BuildAsync(new CharacterId(characterId), cancellationToken);
    return snapshot is null ? Results.NotFound() : Results.Ok(snapshot);
});

app.MapGet("/hotel/public-rooms/{entryId:int}", async (
    int entryId,
    IHotelReadService hotelReadService,
    CancellationToken cancellationToken) =>
{
    PublicRoomHotelSnapshot? snapshot = await hotelReadService.GetPublicRoomSnapshotAsync(entryId, cancellationToken);
    return snapshot is null ? Results.NotFound() : Results.Ok(snapshot);
});

app.MapGet("/hotel/sessions/{characterId:long}", async (
    long characterId,
    IHotelSessionSnapshotService hotelSessionSnapshotService,
    CancellationToken cancellationToken) =>
{
    HotelSessionSnapshot? snapshot = await hotelSessionSnapshotService.BuildAsync(new CharacterId(characterId), cancellationToken);
    return snapshot is null ? Results.NotFound() : Results.Ok(snapshot);
});

app.MapGet("/hotel/support", async (
    ISupportCenterService supportCenterService,
    CancellationToken cancellationToken) =>
{
    SupportCenterSnapshot snapshot = await supportCenterService.GetSnapshotAsync(cancellationToken);
    return Results.Ok(snapshot);
});

app.MapPost("/hotel/support/calls", async (
    HttpContext httpContext,
    SupportCallRequest request,
    ISessionStore sessionStore,
    ISupportCenterService supportCenterService,
    CancellationToken cancellationToken) =>
{
    SessionTicket? session = await ResolveSessionAsync(httpContext, sessionStore, cancellationToken);
    if (session is null)
    {
        return Results.Unauthorized();
    }

    SupportCallRequest authorizedRequest = request with
    {
        SenderCharacterId = new CharacterId(session.CharacterId)
    };

    SupportCallResult result = await supportCenterService.CreateCallAsync(authorizedRequest, cancellationToken);
    return result.Succeeded ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapPost("/hotel/rooms/entry", async (
    HttpContext httpContext,
    RoomEntryRequest request,
    ISessionStore sessionStore,
    IRoomEntryService roomEntryService,
    CancellationToken cancellationToken) =>
{
    SessionTicket? session = await ResolveSessionAsync(httpContext, sessionStore, cancellationToken);
    if (session is null)
    {
        return Results.Unauthorized();
    }

    RoomEntryRequest authorizedRequest = request with
    {
        CharacterId = new CharacterId(session.CharacterId)
    };

    RoomEntryResult result = await roomEntryService.EnterAsync(authorizedRequest, cancellationToken);

    return result.Succeeded
        ? Results.Ok(result)
        : Results.BadRequest(result);
});

app.MapPost("/hotel/rooms/move", async (
    HttpContext httpContext,
    RoomActorMovementRequest request,
    ISessionStore sessionStore,
    IRoomInteractionService roomInteractionService,
    CancellationToken cancellationToken) =>
{
    SessionTicket? session = await ResolveSessionAsync(httpContext, sessionStore, cancellationToken);
    if (session is null)
    {
        return Results.Unauthorized();
    }

    RoomActorMovementRequest authorizedRequest = request with
    {
        CharacterId = new CharacterId(session.CharacterId)
    };

    RoomActorMovementResult result = await roomInteractionService.MoveActorAsync(authorizedRequest, cancellationToken);
    return result.Succeeded ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapPost("/hotel/rooms/chat", async (
    HttpContext httpContext,
    RoomChatRequest request,
    ISessionStore sessionStore,
    IRoomInteractionService roomInteractionService,
    CancellationToken cancellationToken) =>
{
    SessionTicket? session = await ResolveSessionAsync(httpContext, sessionStore, cancellationToken);
    if (session is null)
    {
        return Results.Unauthorized();
    }

    RoomChatRequest authorizedRequest = request with
    {
        CharacterId = new CharacterId(session.CharacterId)
    };

    RoomChatResult result = await roomInteractionService.SendChatAsync(authorizedRequest, cancellationToken);
    return result.Succeeded ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapGet("/hotel/rooms/{roomId:long}/runtime", async (
    long roomId,
    IRoomRuntimeSnapshotService roomRuntimeSnapshotService,
    CancellationToken cancellationToken) =>
{
    RoomRuntimeSnapshot? snapshot = await roomRuntimeSnapshotService.BuildAsync(new RoomId(roomId), cancellationToken);
    return snapshot is null ? Results.NotFound() : Results.Ok(snapshot);
});

app.MapRuntimeDiagnostics();

app.Run();

static async ValueTask<SessionTicket?> ResolveSessionAsync(
    HttpContext httpContext,
    ISessionStore sessionStore,
    CancellationToken cancellationToken)
{
    if (!httpContext.Request.Headers.TryGetValue(SessionTicketHeaderName, out var values))
    {
        return null;
    }

    string? ticket = values.FirstOrDefault();
    if (string.IsNullOrWhiteSpace(ticket))
    {
        return null;
    }

    SessionTicket? session = await sessionStore.FindByTicketAsync(ticket, cancellationToken);
    // Expired tickets are treated as invalid immediately so gateway endpoints
    // never mutate room or support state on behalf of dead sessions.
    if (session is null || session.ExpiresAtUtc <= DateTime.UtcNow)
    {
        return null;
    }

    return session;
}

static string ResolveInformationalVersion(Assembly assembly)
{
    return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? assembly.GetName().Version?.ToString()
        ?? "unknown";
}
