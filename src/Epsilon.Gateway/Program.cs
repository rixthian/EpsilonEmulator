using Epsilon.Auth;
using Epsilon.CoreGame;
using Epsilon.Games;
using Epsilon.Gateway;
using Epsilon.Persistence;
using Epsilon.Protocol;
using System.Reflection;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);
// Mutable hotel actions are bound to a server-issued session ticket instead of
// trusting caller-supplied character ids in request bodies.
const string SessionTicketHeaderName = "X-Epsilon-Session-Ticket";

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

AddRootConfiguration(builder.Configuration, builder.Environment.EnvironmentName, "gateway");
builder.Configuration.AddEnvironmentVariables(prefix: "EPSILON_");

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
    options.IncludeScopes = false;
});
builder.Logging.SetMinimumLevel(LogLevel.Information);
builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore.Routing.EndpointMiddleware", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore.Http.Result", LogLevel.Warning);

builder.Services.AddGatewayRuntime(builder.Configuration);
builder.Services.AddPersistenceRuntime(builder.Configuration);
builder.Services.AddAuthRuntime(builder.Configuration);
builder.Services.AddCoreGameRuntime();
builder.Services.AddGameRuntime();
builder.Services.AddProtocolServices(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

StartupBanner.Print(
    app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<GatewayRuntimeOptions>>().Value,
    app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<PersistenceOptions>>().Value,
    app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<AuthOptions>>().Value,
    app.Services.GetRequiredService<PacketRegistry>(),
    ResolveInformationalVersion(typeof(GatewayRuntimeOptions).Assembly),
    app.Environment.EnvironmentName);

GatewayConsole.WriteEvent(
    GatewayConsoleEventKind.Packet,
    $"packet handlers ready: inbound={app.Services.GetRequiredService<PacketRegistry>().Incoming.Count}, outbound={app.Services.GetRequiredService<PacketRegistry>().Outgoing.Count}");

GatewayConsole.WriteEvent(
    GatewayConsoleEventKind.Security,
    $"session mode: {DescribeSessionMode(app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<AuthOptions>>().Value)}");

app.Lifetime.ApplicationStarted.Register(() =>
{
    GatewayConsole.WriteEvent(GatewayConsoleEventKind.Ok, "gateway startup completed");
});

app.Lifetime.ApplicationStopping.Register(() =>
{
    GatewayConsole.WriteEvent(GatewayConsoleEventKind.Alert, "gateway shutdown requested");
});

// Packet logging middleware — runs on every request and records a lightweight
// entry (no body capture) for diagnostic inspection.
app.Use(async (context, next) =>
{
    var packetLogger = context.RequestServices.GetRequiredService<IPacketLogger>();
    var sw = System.Diagnostics.Stopwatch.StartNew();

    // Attempt to extract character id from the session ticket header before
    // the handler runs, so we have it even if the request is ultimately rejected.
    long? characterId = null;
    if (context.Request.Headers.TryGetValue(SessionTicketHeaderName, out var ticketValues))
    {
        var sessionStore = context.RequestServices.GetRequiredService<ISessionStore>();
        string? ticket = ticketValues.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(ticket))
        {
            SessionTicket? session = await sessionStore.FindByTicketAsync(ticket, context.RequestAborted);
            if (session is not null && session.ExpiresAtUtc > DateTime.UtcNow)
            {
                characterId = session.CharacterId;
            }
        }
    }

    await next(context);
    sw.Stop();

    string endpointName = context.GetEndpoint()?.DisplayName ?? context.Request.Path.Value ?? "unknown";
    packetLogger.Log(new PacketLogEntry(
        TimestampUtc: DateTime.UtcNow,
        Direction: "inbound",
        EndpointName: endpointName,
        CharacterId: characterId,
        RemoteAddress: context.Connection.RemoteIpAddress?.ToString(),
        ResponseStatusCode: context.Response.StatusCode,
        ElapsedMs: sw.ElapsedMilliseconds));

    if (context.Response.StatusCode >= StatusCodes.Status500InternalServerError)
    {
        GatewayConsole.WriteEvent(
            GatewayConsoleEventKind.Error,
            $"{endpointName} -> {context.Response.StatusCode} in {sw.ElapsedMilliseconds}ms");
    }
    else if (context.Response.StatusCode is StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden)
    {
        GatewayConsole.WriteEvent(
            GatewayConsoleEventKind.Security,
            $"{endpointName} -> {context.Response.StatusCode} from {context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}");
    }
    else if (context.Response.StatusCode >= StatusCodes.Status400BadRequest)
    {
        GatewayConsole.WriteEvent(
            GatewayConsoleEventKind.Warning,
            $"{endpointName} -> {context.Response.StatusCode} in {sw.ElapsedMilliseconds}ms");
    }
    else if (sw.ElapsedMilliseconds >= 1000)
    {
        GatewayConsole.WriteEvent(
            GatewayConsoleEventKind.Time,
            $"{endpointName} -> {context.Response.StatusCode} slow path {sw.ElapsedMilliseconds}ms");
    }
});

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

app.MapPost("/auth/register", async (
    RegistrationRequest request,
    IRegistrationService registrationService,
    CancellationToken cancellationToken) =>
{
    RegistrationResult result = await registrationService.RegisterAsync(request, cancellationToken);

    return result.Succeeded
        ? Results.Ok(result)
        : Results.BadRequest(result);
});

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

app.MapGet("/hotel/connection", async (
    HttpContext httpContext,
    ISessionStore sessionStore,
    IRoomRuntimeRepository roomRuntimeRepository,
    CancellationToken cancellationToken) =>
{
    SessionTicket? session = await ResolveSessionAsync(httpContext, sessionStore, cancellationToken);
    if (session is null)
    {
        return Results.Unauthorized();
    }

    RoomId? roomId = await ResolveCurrentRoomIdAsync(
        new CharacterId(session.CharacterId),
        roomRuntimeRepository,
        cancellationToken);

    return Results.Ok(BuildConnectionSnapshot(session, roomId));
});

app.MapPost("/hotel/connection/heartbeat", async (
    HttpContext httpContext,
    ISessionStore sessionStore,
    IRoomRuntimeRepository roomRuntimeRepository,
    CancellationToken cancellationToken) =>
{
    SessionTicket? session = await ResolveSessionAsync(httpContext, sessionStore, cancellationToken);
    if (session is null)
    {
        return Results.Unauthorized();
    }

    SessionTicket? updatedSession = await sessionStore.TouchAsync(
        session.Ticket,
        httpContext.Connection.RemoteIpAddress?.ToString(),
        cancellationToken);
    if (updatedSession is null)
    {
        return Results.Unauthorized();
    }

    RoomId? roomId = await ResolveCurrentRoomIdAsync(
        new CharacterId(updatedSession.CharacterId),
        roomRuntimeRepository,
        cancellationToken);

    return Results.Ok(BuildConnectionSnapshot(updatedSession, roomId));
});

app.MapPost("/hotel/connection/disconnect", async (
    HttpContext httpContext,
    ISessionStore sessionStore,
    IRoomRuntimeRepository roomRuntimeRepository,
    IRoomRuntimeCoordinator roomRuntimeCoordinator,
    CancellationToken cancellationToken) =>
{
    SessionTicket? session = await ResolveSessionAsync(httpContext, sessionStore, cancellationToken);
    if (session is null)
    {
        return Results.Unauthorized();
    }

    IReadOnlyList<RoomId> removedRoomIds = await roomRuntimeRepository.RemoveActorFromAllRoomsAsync(
        session.CharacterId,
        cancellationToken);

    foreach (RoomId roomId in removedRoomIds)
    {
        await roomRuntimeCoordinator.SignalMutationAsync(
            roomId,
            RoomRuntimeMutationKind.ActorPresenceChanged,
            cancellationToken);
    }

    bool revoked = await sessionStore.RevokeAsync(session.Ticket, cancellationToken);

    return Results.Ok(new
    {
        disconnected = revoked,
        characterId = session.CharacterId,
        removedRoomIds = removedRoomIds.Select(static roomId => roomId.Value).ToArray(),
        disconnectedAtUtc = DateTime.UtcNow
    });
});

app.MapPost("/protocol/execute/{commandName}", async (
    HttpContext httpContext,
    string commandName,
    ProtocolExecuteInput input,
    ProtocolCommandRegistry protocolCommandRegistry,
    ISessionStore sessionStore,
    IHotelReadService hotelReadService,
    IRoomEntryService roomEntryService,
    IRoomInteractionService roomInteractionService,
    CancellationToken cancellationToken) =>
{
    if (!protocolCommandRegistry.TryGet(commandName, out ProtocolCommandDefinition? command) || command is null)
    {
        return Results.BadRequest(new { error = "unknown_protocol_command", commandName });
    }

    Dictionary<string, System.Text.Json.JsonElement> arguments =
        input.Arguments ?? new Dictionary<string, System.Text.Json.JsonElement>(StringComparer.OrdinalIgnoreCase);

    return command.Name switch
    {
        "session.initialize_crypto" => Results.Ok(new
        {
            command = command.Name,
            packetName = command.PacketName,
            flow = command.Flow,
            protocolFamily = protocolCommandRegistry.Family,
            protocolRevision = protocolCommandRegistry.Revision,
            crypto = "session-bootstrap"
        }),
        "session.authenticate_with_ticket" => await ExecuteTicketAuthenticationAsync(
            httpContext,
            sessionStore,
            cancellationToken),
        "identity.get_self" => await ExecuteGetSelfAsync(
            httpContext,
            sessionStore,
            hotelReadService,
            cancellationToken),
        "rooms.enter_room" => await ExecuteProtocolRoomEntryAsync(
            httpContext,
            sessionStore,
            roomEntryService,
            arguments,
            cancellationToken),
        "rooms.move_avatar" => await ExecuteProtocolMoveAsync(
            httpContext,
            sessionStore,
            roomInteractionService,
            arguments,
            cancellationToken),
        "rooms.chat" => await ExecuteProtocolChatAsync(
            httpContext,
            sessionStore,
            roomInteractionService,
            arguments,
            cancellationToken),
        _ => Results.BadRequest(new { error = "unsupported_protocol_command", commandName = command.Name })
    };
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

app.MapGet("/hotel/groups", async (
    HttpContext httpContext,
    ISessionStore sessionStore,
    IGroupService groupService,
    CancellationToken cancellationToken) =>
{
    SessionTicket? session = await ResolveSessionAsync(httpContext, sessionStore, cancellationToken);
    if (session is null)
    {
        return Results.Unauthorized();
    }

    IReadOnlyList<HotelGroupSummary> groups =
        await groupService.ListAsync(new CharacterId(session.CharacterId), cancellationToken);
    return Results.Ok(groups);
});

app.MapGet("/hotel/groups/{groupId:long}", async (
    HttpContext httpContext,
    long groupId,
    ISessionStore sessionStore,
    IGroupService groupService,
    CancellationToken cancellationToken) =>
{
    SessionTicket? session = await ResolveSessionAsync(httpContext, sessionStore, cancellationToken);
    if (session is null)
    {
        return Results.Unauthorized();
    }

    HotelGroupSnapshot? snapshot =
        await groupService.GetAsync(new GroupId(groupId), new CharacterId(session.CharacterId), cancellationToken);
    return snapshot is null ? Results.NotFound() : Results.Ok(snapshot);
});

app.MapPost("/hotel/groups", async (
    HttpContext httpContext,
    CreateGroupInput input,
    ISessionStore sessionStore,
    IGroupService groupService,
    CancellationToken cancellationToken) =>
{
    SessionTicket? session = await ResolveSessionAsync(httpContext, sessionStore, cancellationToken);
    if (session is null)
    {
        return Results.Unauthorized();
    }

    GroupJoinMode joinMode = ParseGroupJoinMode(input.JoinMode);
    CreateGroupResult result = await groupService.CreateAsync(
        new CreateGroupRequest(
            new CharacterId(session.CharacterId),
            input.Name,
            input.Description,
            input.BadgeCode,
            input.RoomId is null ? null : new RoomId(input.RoomId.Value),
            joinMode),
        cancellationToken);

    return result.Succeeded
        ? Results.Ok(result.Snapshot)
        : Results.BadRequest(result);
});

app.MapPost("/hotel/groups/{groupId:long}/join", async (
    HttpContext httpContext,
    long groupId,
    ISessionStore sessionStore,
    IGroupService groupService,
    CancellationToken cancellationToken) =>
{
    SessionTicket? session = await ResolveSessionAsync(httpContext, sessionStore, cancellationToken);
    if (session is null)
    {
        return Results.Unauthorized();
    }

    HotelGroupSnapshot? snapshot =
        await groupService.JoinAsync(new GroupId(groupId), new CharacterId(session.CharacterId), cancellationToken);
    return snapshot is null ? Results.NotFound() : Results.Ok(snapshot);
});

app.MapPost("/hotel/groups/{groupId:long}/leave", async (
    HttpContext httpContext,
    long groupId,
    ISessionStore sessionStore,
    IGroupService groupService,
    CancellationToken cancellationToken) =>
{
    SessionTicket? session = await ResolveSessionAsync(httpContext, sessionStore, cancellationToken);
    if (session is null)
    {
        return Results.Unauthorized();
    }

    bool left = await groupService.LeaveAsync(new GroupId(groupId), new CharacterId(session.CharacterId), cancellationToken);
    return left ? Results.Ok(new { left = true, groupId }) : Results.BadRequest(new { left = false, groupId });
});

app.MapPost("/hotel/groups/{groupId:long}/room", async (
    HttpContext httpContext,
    long groupId,
    SetGroupRoomInput input,
    ISessionStore sessionStore,
    IGroupService groupService,
    CancellationToken cancellationToken) =>
{
    SessionTicket? session = await ResolveSessionAsync(httpContext, sessionStore, cancellationToken);
    if (session is null)
    {
        return Results.Unauthorized();
    }

    HotelGroupSnapshot? snapshot = await groupService.SetLinkedRoomAsync(
        new GroupId(groupId),
        new CharacterId(session.CharacterId),
        input.RoomId is null ? null : new RoomId(input.RoomId.Value),
        cancellationToken);

    return snapshot is null
        ? Results.BadRequest(new { error = "group_room_link_rejected" })
        : Results.Ok(snapshot);
});

app.MapGet("/hotel/catalog/pages", async (
    HttpContext httpContext,
    ISessionStore sessionStore,
    IHotelCommerceService hotelCommerceService,
    CancellationToken cancellationToken) =>
{
    SessionTicket? session = await ResolveSessionAsync(httpContext, sessionStore, cancellationToken);
    if (session is null)
    {
        return Results.Unauthorized();
    }

    IReadOnlyList<CatalogPageSnapshot> pages = await hotelCommerceService.GetCatalogPagesAsync(
        new CharacterId(session.CharacterId),
        cancellationToken);
    return Results.Ok(pages);
});

app.MapGet("/hotel/catalog/pages/{catalogPageId:long}", async (
    HttpContext httpContext,
    long catalogPageId,
    ISessionStore sessionStore,
    IHotelCommerceService hotelCommerceService,
    CancellationToken cancellationToken) =>
{
    SessionTicket? session = await ResolveSessionAsync(httpContext, sessionStore, cancellationToken);
    if (session is null)
    {
        return Results.Unauthorized();
    }

    CatalogPageSnapshot? page = await hotelCommerceService.GetCatalogPageAsync(
        new CharacterId(session.CharacterId),
        new CatalogPageId(catalogPageId),
        cancellationToken);
    return page is null ? Results.NotFound() : Results.Ok(page);
});

app.MapGet("/hotel/catalog/landing", async (
    HttpContext httpContext,
    ISessionStore sessionStore,
    IHotelPresentationService hotelPresentationService,
    CancellationToken cancellationToken) =>
{
    SessionTicket? session = await ResolveSessionAsync(httpContext, sessionStore, cancellationToken);
    if (session is null)
    {
        return Results.Unauthorized();
    }

    CatalogLandingSnapshot landing = await hotelPresentationService.GetCatalogLandingAsync(
        new CharacterId(session.CharacterId),
        cancellationToken);
    return Results.Ok(landing);
});

app.MapGet("/hotel/games", async (
    IHotelWorldFeatureService hotelWorldFeatureService,
    CancellationToken cancellationToken) =>
{
    GameCatalogSnapshot snapshot = await hotelWorldFeatureService.GetGameCatalogAsync(cancellationToken);
    return Results.Ok(snapshot);
});

app.MapGet("/hotel/games/sessions", async (
    IGameRuntimeService gameRuntimeService,
    CancellationToken cancellationToken) =>
{
    IReadOnlyList<GameSessionState> sessions = await gameRuntimeService.GetActiveSessionsAsync(cancellationToken);
    return Results.Ok(new GameRuntimeCatalogSnapshot(sessions));
});

app.MapGet("/hotel/games/sessions/{sessionKey}", async (
    string sessionKey,
    IGameRuntimeService gameRuntimeService,
    CancellationToken cancellationToken) =>
{
    GameSessionState? session = await gameRuntimeService.GetSessionAsync(sessionKey, cancellationToken);
    return session is null ? Results.NotFound() : Results.Ok(session);
});

app.MapPost("/hotel/games/battleball/sessions/{sessionKey}/prepare", async (
    HttpContext httpContext,
    string sessionKey,
    ISessionStore sessionStore,
    IAccessControlService accessControlService,
    IBattleBallLifecycleService battleBallLifecycleService,
    CancellationToken cancellationToken) =>
{
    SessionTicket? session = await ResolveSessionAsync(httpContext, sessionStore, cancellationToken);
    if (session is null)
    {
        return Results.Unauthorized();
    }

    if (!await RequireCapabilityAsync(accessControlService, session, StaffCapabilityKeys.GamesManage, cancellationToken))
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    GameSessionUpdateResult result = await battleBallLifecycleService.PrepareMatchAsync(sessionKey, cancellationToken);
    return result.Succeeded ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapPost("/hotel/games/battleball/sessions/{sessionKey}/start", async (
    HttpContext httpContext,
    string sessionKey,
    ISessionStore sessionStore,
    IAccessControlService accessControlService,
    IBattleBallLifecycleService battleBallLifecycleService,
    CancellationToken cancellationToken) =>
{
    SessionTicket? session = await ResolveSessionAsync(httpContext, sessionStore, cancellationToken);
    if (session is null)
    {
        return Results.Unauthorized();
    }

    if (!await RequireCapabilityAsync(accessControlService, session, StaffCapabilityKeys.GamesManage, cancellationToken))
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    GameSessionUpdateResult result = await battleBallLifecycleService.StartRoundAsync(sessionKey, cancellationToken);
    return result.Succeeded ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapPost("/hotel/games/battleball/sessions/{sessionKey}/score", async (
    HttpContext httpContext,
    string sessionKey,
    BattleBallScoreInput input,
    ISessionStore sessionStore,
    IAccessControlService accessControlService,
    IBattleBallLifecycleService battleBallLifecycleService,
    CancellationToken cancellationToken) =>
{
    SessionTicket? session = await ResolveSessionAsync(httpContext, sessionStore, cancellationToken);
    if (session is null)
    {
        return Results.Unauthorized();
    }

    if (!await RequireCapabilityAsync(accessControlService, session, StaffCapabilityKeys.GamesManage, cancellationToken))
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    GameSessionUpdateResult result = await battleBallLifecycleService.AwardPointsAsync(
        sessionKey,
        input.TeamKey,
        input.Points,
        cancellationToken);
    return result.Succeeded ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapPost("/hotel/games/battleball/sessions/{sessionKey}/finish", async (
    HttpContext httpContext,
    string sessionKey,
    ISessionStore sessionStore,
    IAccessControlService accessControlService,
    IBattleBallLifecycleService battleBallLifecycleService,
    CancellationToken cancellationToken) =>
{
    SessionTicket? session = await ResolveSessionAsync(httpContext, sessionStore, cancellationToken);
    if (session is null)
    {
        return Results.Unauthorized();
    }

    if (!await RequireCapabilityAsync(accessControlService, session, StaffCapabilityKeys.GamesManage, cancellationToken))
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    GameSessionUpdateResult result = await battleBallLifecycleService.FinishMatchAsync(sessionKey, cancellationToken);
    return result.Succeeded ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapGet("/hotel/commerce/features", async (
    IHotelCommerceFeatureService hotelCommerceFeatureService,
    CancellationToken cancellationToken) =>
{
    HotelCommerceFeatureSnapshot snapshot = await hotelCommerceFeatureService.GetSnapshotAsync(cancellationToken);
    return Results.Ok(snapshot);
});

app.MapGet("/hotel/public-rooms/{entryId:int}/behaviors", async (
    int entryId,
    IHotelWorldFeatureService hotelWorldFeatureService,
    CancellationToken cancellationToken) =>
{
    PublicRoomBehaviorSnapshot? snapshot =
        await hotelWorldFeatureService.GetPublicRoomBehaviorSnapshotAsync(entryId, cancellationToken);
    return snapshot is null ? Results.NotFound() : Results.Ok(snapshot);
});

app.MapGet("/hotel/navigator/public-rooms", async (
    string? q,
    bool recommended,
    IHotelNavigatorService hotelNavigatorService,
    CancellationToken cancellationToken) =>
{
    NavigatorSearchSnapshot snapshot = await hotelNavigatorService.SearchPublicRoomsAsync(
        new NavigatorSearchRequest(q, recommended),
        cancellationToken);
    return Results.Ok(snapshot);
});

app.MapGet("/hotel/preferences/interface", async (
    HttpContext httpContext,
    ISessionStore sessionStore,
    IInterfacePreferenceService interfacePreferenceService,
    CancellationToken cancellationToken) =>
{
    SessionTicket? session = await ResolveSessionAsync(httpContext, sessionStore, cancellationToken);
    if (session is null)
    {
        return Results.Unauthorized();
    }

    InterfacePreferenceSnapshot snapshot = await interfacePreferenceService.GetSnapshotAsync(
        new CharacterId(session.CharacterId),
        cancellationToken);
    return Results.Ok(snapshot);
});

app.MapGet("/hotel/badges", async (
    string? query,
    int? take,
    IBadgeCatalogService badgeCatalogService,
    CancellationToken cancellationToken) =>
{
    IReadOnlyList<Epsilon.Content.BadgeDefinition> badges = await badgeCatalogService.SearchAsync(
        query,
        take ?? 100,
        cancellationToken);
    return Results.Ok(badges);
});

app.MapGet("/hotel/badges/{badgeCode}", async (
    string badgeCode,
    IBadgeCatalogService badgeCatalogService,
    CancellationToken cancellationToken) =>
{
    Epsilon.Content.BadgeDefinition? badge = await badgeCatalogService.GetBadgeAsync(badgeCode, cancellationToken);
    return badge is null ? Results.NotFound() : Results.Ok(badge);
});

app.MapPut("/hotel/preferences/interface-language", async (
    HttpContext httpContext,
    UpdateInterfaceLanguageRequest request,
    ISessionStore sessionStore,
    IInterfacePreferenceService interfacePreferenceService,
    CancellationToken cancellationToken) =>
{
    SessionTicket? session = await ResolveSessionAsync(httpContext, sessionStore, cancellationToken);
    if (session is null)
    {
        return Results.Unauthorized();
    }

    try
    {
        InterfacePreferenceSnapshot snapshot = await interfacePreferenceService.SetLanguageAsync(
            new CharacterId(session.CharacterId),
            request.LanguageCode,
            cancellationToken);
        return Results.Ok(snapshot);
    }
    catch (InvalidOperationException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
});

app.MapGet("/hotel/inventory", async (
    HttpContext httpContext,
    ISessionStore sessionStore,
    IHotelCommerceService hotelCommerceService,
    CancellationToken cancellationToken) =>
{
    SessionTicket? session = await ResolveSessionAsync(httpContext, sessionStore, cancellationToken);
    if (session is null)
    {
        return Results.Unauthorized();
    }

    InventorySnapshot inventory = await hotelCommerceService.GetInventoryAsync(
        new CharacterId(session.CharacterId),
        cancellationToken);
    return Results.Ok(inventory);
});

app.MapPost("/hotel/catalog/purchase", async (
    HttpContext httpContext,
    CatalogPurchaseInput input,
    ISessionStore sessionStore,
    IHotelCommerceService hotelCommerceService,
    CancellationToken cancellationToken) =>
{
    SessionTicket? session = await ResolveSessionAsync(httpContext, sessionStore, cancellationToken);
    if (session is null)
    {
        return Results.Unauthorized();
    }

    CatalogPurchaseRequest authorizedRequest = new(
        new CharacterId(session.CharacterId),
        new CatalogOfferId(input.CatalogOfferId));

    CatalogPurchaseResult result = await hotelCommerceService.PurchaseAsync(authorizedRequest, cancellationToken);
    return result.Succeeded ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapPost("/hotel/catalog/redeem-voucher", async (
    HttpContext httpContext,
    RedeemVoucherInput input,
    ISessionStore sessionStore,
    IHotelCommerceService hotelCommerceService,
    CancellationToken cancellationToken) =>
{
    SessionTicket? session = await ResolveSessionAsync(httpContext, sessionStore, cancellationToken);
    if (session is null)
    {
        return Results.Unauthorized();
    }

    RedeemVoucherResult result = await hotelCommerceService.RedeemVoucherAsync(
        new RedeemVoucherRequest(new CharacterId(session.CharacterId), input.VoucherCode),
        cancellationToken);
    return result.Succeeded ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapGet("/hotel/support", async (
    HttpContext httpContext,
    ISessionStore sessionStore,
    ISupportCenterService supportCenterService,
    CancellationToken cancellationToken) =>
{
    SessionTicket? session = await ResolveSessionAsync(httpContext, sessionStore, cancellationToken);
    if (session is null)
    {
        return Results.Unauthorized();
    }

    SupportCenterSnapshot snapshot = await supportCenterService.GetSnapshotAsync(cancellationToken);
    return Results.Ok(snapshot);
});

app.MapPost("/hotel/support/calls", async (
    HttpContext httpContext,
    SupportCallInput input,
    ISessionStore sessionStore,
    ISupportCenterService supportCenterService,
    CancellationToken cancellationToken) =>
{
    SessionTicket? session = await ResolveSessionAsync(httpContext, sessionStore, cancellationToken);
    if (session is null)
    {
        return Results.Unauthorized();
    }

    SupportCallRequest authorizedRequest = new(
        SenderCharacterId: new CharacterId(session.CharacterId),
        CategoryId: input.CategoryId,
        Message: input.Message,
        ReportedCharacterId: input.ReportedCharacterId is null ? null : new CharacterId(input.ReportedCharacterId.Value),
        RoomId: input.RoomId is null ? null : new RoomId(input.RoomId.Value));

    SupportCallResult result = await supportCenterService.CreateCallAsync(authorizedRequest, cancellationToken);
    return result.Succeeded ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapPost("/hotel/rooms/entry", async (
    HttpContext httpContext,
    RoomEntryInput input,
    ISessionStore sessionStore,
    IRoomEntryService roomEntryService,
    CancellationToken cancellationToken) =>
{
    SessionTicket? session = await ResolveSessionAsync(httpContext, sessionStore, cancellationToken);
    if (session is null)
    {
        return Results.Unauthorized();
    }

    RoomEntryRequest authorizedRequest = new(
        CharacterId: new CharacterId(session.CharacterId),
        RoomId: new RoomId(input.RoomId),
        Password: input.Password,
        SpectatorMode: input.SpectatorMode);

    RoomEntryResult result = await roomEntryService.EnterAsync(authorizedRequest, cancellationToken);

    return result.Succeeded
        ? Results.Ok(result)
        : Results.BadRequest(result);
});

app.MapPost("/hotel/rooms/move", async (
    HttpContext httpContext,
    RoomMoveInput input,
    ISessionStore sessionStore,
    IRoomInteractionService roomInteractionService,
    CancellationToken cancellationToken) =>
{
    SessionTicket? session = await ResolveSessionAsync(httpContext, sessionStore, cancellationToken);
    if (session is null)
    {
        return Results.Unauthorized();
    }

    RoomActorMovementRequest authorizedRequest = new(
        CharacterId: new CharacterId(session.CharacterId),
        RoomId: new RoomId(input.RoomId),
        DestinationX: input.DestinationX,
        DestinationY: input.DestinationY);

    RoomActorMovementResult result = await roomInteractionService.MoveActorAsync(authorizedRequest, cancellationToken);
    return result.Succeeded ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapPost("/hotel/rooms/chat", async (
    HttpContext httpContext,
    RoomChatInput input,
    ISessionStore sessionStore,
    IRoomInteractionService roomInteractionService,
    CancellationToken cancellationToken) =>
{
    SessionTicket? session = await ResolveSessionAsync(httpContext, sessionStore, cancellationToken);
    if (session is null)
    {
        return Results.Unauthorized();
    }

    RoomChatRequest authorizedRequest = new(
        CharacterId: new CharacterId(session.CharacterId),
        RoomId: new RoomId(input.RoomId),
        Message: input.Message);

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

app.MapGet("/hotel/rooms/{roomId:long}/visuals", async (
    long roomId,
    IHotelPresentationService hotelPresentationService,
    CancellationToken cancellationToken) =>
{
    RoomVisualSnapshot? snapshot = await hotelPresentationService.GetRoomVisualSnapshotAsync(
        new RoomId(roomId),
        cancellationToken);
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

static ValueTask<bool> RequireCapabilityAsync(
    IAccessControlService accessControlService,
    SessionTicket session,
    string capabilityKey,
    CancellationToken cancellationToken)
{
    return accessControlService.HasCapabilityAsync(new CharacterId(session.CharacterId), capabilityKey, cancellationToken);
}

static string ResolveInformationalVersion(Assembly assembly)
{
    return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? assembly.GetName().Version?.ToString()
        ?? "unknown";
}

static string DescribeSessionMode(AuthOptions authOptions)
{
    if (!string.IsNullOrWhiteSpace(authOptions.RedisConnectionString) && !authOptions.AllowInMemorySessions)
    {
        return "redis_shared";
    }

    return authOptions.AllowInMemorySessions ? "in_memory" : "custom";
}

static GroupJoinMode ParseGroupJoinMode(string? rawValue)
{
    return Enum.TryParse<GroupJoinMode>(rawValue, true, out GroupJoinMode joinMode)
        ? joinMode
        : GroupJoinMode.Open;
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

static async ValueTask<RoomId?> ResolveCurrentRoomIdAsync(
    CharacterId characterId,
    IRoomRuntimeRepository roomRuntimeRepository,
    CancellationToken cancellationToken)
{
    IReadOnlyList<RoomId> activeRoomIds = await roomRuntimeRepository.GetAllActiveRoomIdsAsync(cancellationToken);
    foreach (RoomId roomId in activeRoomIds)
    {
        RoomActorState? actor = await roomRuntimeRepository.GetActorByIdAsync(
            roomId,
            characterId.Value,
            cancellationToken);
        if (actor is not null)
        {
            return roomId;
        }
    }

    return null;
}

static object BuildConnectionSnapshot(SessionTicket session, RoomId? roomId)
{
    return new
    {
        sessionId = session.SessionId,
        accountId = session.AccountId,
        characterId = session.CharacterId,
        ticket = session.Ticket,
        remoteAddress = session.RemoteAddress,
        createdAtUtc = session.CreatedAtUtc,
        lastSeenAtUtc = session.LastSeenAtUtc,
        expiresAtUtc = session.ExpiresAtUtc,
        connected = true,
        currentRoomId = roomId?.Value
    };
}

static async Task<IResult> ExecuteTicketAuthenticationAsync(
    HttpContext httpContext,
    ISessionStore sessionStore,
    CancellationToken cancellationToken)
{
    SessionTicket? session = await ResolveSessionAsync(httpContext, sessionStore, cancellationToken);
    return session is null
        ? Results.Unauthorized()
        : Results.Ok(new
        {
            authenticated = true,
            session = session
        });
}

static async Task<IResult> ExecuteGetSelfAsync(
    HttpContext httpContext,
    ISessionStore sessionStore,
    IHotelReadService hotelReadService,
    CancellationToken cancellationToken)
{
    SessionTicket? session = await ResolveSessionAsync(httpContext, sessionStore, cancellationToken);
    if (session is null)
    {
        return Results.Unauthorized();
    }

    CharacterHotelSnapshot? snapshot = await hotelReadService.GetCharacterSnapshotAsync(
        new CharacterId(session.CharacterId),
        cancellationToken);
    return snapshot is null ? Results.NotFound() : Results.Ok(snapshot);
}

static async Task<IResult> ExecuteProtocolRoomEntryAsync(
    HttpContext httpContext,
    ISessionStore sessionStore,
    IRoomEntryService roomEntryService,
    IReadOnlyDictionary<string, System.Text.Json.JsonElement> arguments,
    CancellationToken cancellationToken)
{
    SessionTicket? session = await ResolveSessionAsync(httpContext, sessionStore, cancellationToken);
    if (session is null)
    {
        return Results.Unauthorized();
    }

    if (!TryReadInt64(arguments, "roomId", out long roomId))
    {
        return Results.BadRequest(new { error = "roomId_required" });
    }

    string? password = TryReadString(arguments, "password");
    bool spectatorMode = TryReadBoolean(arguments, "spectatorMode");

    RoomEntryResult result = await roomEntryService.EnterAsync(
        new RoomEntryRequest(new CharacterId(session.CharacterId), new RoomId(roomId), password, spectatorMode),
        cancellationToken);

    return result.Succeeded ? Results.Ok(result) : Results.BadRequest(result);
}

static async Task<IResult> ExecuteProtocolMoveAsync(
    HttpContext httpContext,
    ISessionStore sessionStore,
    IRoomInteractionService roomInteractionService,
    IReadOnlyDictionary<string, System.Text.Json.JsonElement> arguments,
    CancellationToken cancellationToken)
{
    SessionTicket? session = await ResolveSessionAsync(httpContext, sessionStore, cancellationToken);
    if (session is null)
    {
        return Results.Unauthorized();
    }

    if (!TryReadInt64(arguments, "roomId", out long roomId) ||
        !TryReadInt32(arguments, "destinationX", out int destinationX) ||
        !TryReadInt32(arguments, "destinationY", out int destinationY))
    {
        return Results.BadRequest(new { error = "roomId_destinationX_destinationY_required" });
    }

    RoomActorMovementResult result = await roomInteractionService.MoveActorAsync(
        new RoomActorMovementRequest(new CharacterId(session.CharacterId), new RoomId(roomId), destinationX, destinationY),
        cancellationToken);

    return result.Succeeded ? Results.Ok(result) : Results.BadRequest(result);
}

static async Task<IResult> ExecuteProtocolChatAsync(
    HttpContext httpContext,
    ISessionStore sessionStore,
    IRoomInteractionService roomInteractionService,
    IReadOnlyDictionary<string, System.Text.Json.JsonElement> arguments,
    CancellationToken cancellationToken)
{
    SessionTicket? session = await ResolveSessionAsync(httpContext, sessionStore, cancellationToken);
    if (session is null)
    {
        return Results.Unauthorized();
    }

    if (!TryReadInt64(arguments, "roomId", out long roomId) ||
        string.IsNullOrWhiteSpace(TryReadString(arguments, "message")))
    {
        return Results.BadRequest(new { error = "roomId_message_required" });
    }

    RoomChatResult result = await roomInteractionService.SendChatAsync(
        new RoomChatRequest(new CharacterId(session.CharacterId), new RoomId(roomId), TryReadString(arguments, "message")!),
        cancellationToken);

    return result.Succeeded ? Results.Ok(result) : Results.BadRequest(result);
}

static bool TryReadInt64(
    IReadOnlyDictionary<string, System.Text.Json.JsonElement> arguments,
    string key,
    out long value)
{
    value = default;
    return arguments.TryGetValue(key, out System.Text.Json.JsonElement argument) && argument.TryGetInt64(out value);
}

static bool TryReadInt32(
    IReadOnlyDictionary<string, System.Text.Json.JsonElement> arguments,
    string key,
    out int value)
{
    value = default;
    return arguments.TryGetValue(key, out System.Text.Json.JsonElement argument) && argument.TryGetInt32(out value);
}

static string? TryReadString(
    IReadOnlyDictionary<string, System.Text.Json.JsonElement> arguments,
    string key)
{
    if (!arguments.TryGetValue(key, out System.Text.Json.JsonElement argument))
    {
        return null;
    }

    return argument.ValueKind == System.Text.Json.JsonValueKind.String
        ? argument.GetString()
        : argument.ToString();
}

static bool TryReadBoolean(
    IReadOnlyDictionary<string, System.Text.Json.JsonElement> arguments,
    string key)
{
    return arguments.TryGetValue(key, out System.Text.Json.JsonElement argument) &&
           argument.ValueKind == System.Text.Json.JsonValueKind.True
        ? true
        : arguments.TryGetValue(key, out argument) &&
          argument.ValueKind == System.Text.Json.JsonValueKind.False
            ? false
            : arguments.TryGetValue(key, out argument) &&
              argument.ValueKind == System.Text.Json.JsonValueKind.String &&
              bool.TryParse(argument.GetString(), out bool parsed) &&
              parsed;
}
