using Epsilon.Auth;
using Epsilon.CoreGame;
using Epsilon.Persistence;
using Epsilon.Protocol;

var builder = WebApplication.CreateBuilder(args);

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
    SupportCallRequest request,
    ISupportCenterService supportCenterService,
    CancellationToken cancellationToken) =>
{
    SupportCallResult result = await supportCenterService.CreateCallAsync(request, cancellationToken);
    return result.Succeeded ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapPost("/hotel/rooms/entry", async (
    RoomEntryRequest request,
    IRoomEntryService roomEntryService,
    CancellationToken cancellationToken) =>
{
    RoomEntryResult result = await roomEntryService.EnterAsync(request, cancellationToken);

    return result.Succeeded
        ? Results.Ok(result)
        : Results.BadRequest(result);
});

app.MapPost("/hotel/rooms/move", async (
    RoomActorMovementRequest request,
    IRoomInteractionService roomInteractionService,
    CancellationToken cancellationToken) =>
{
    RoomActorMovementResult result = await roomInteractionService.MoveActorAsync(request, cancellationToken);
    return result.Succeeded ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapPost("/hotel/rooms/chat", async (
    RoomChatRequest request,
    IRoomInteractionService roomInteractionService,
    CancellationToken cancellationToken) =>
{
    RoomChatResult result = await roomInteractionService.SendChatAsync(request, cancellationToken);
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
