using System.Text.Json;
using Epsilon.Auth;
using Epsilon.CoreGame;
using Epsilon.Games;
using Epsilon.Protocol;

namespace Epsilon.Gateway;

public sealed class ProtocolCommandExecutionService
{
    private readonly ProtocolCommandRegistry _protocolCommandRegistry;
    private readonly ISessionStore _sessionStore;
    private readonly IHotelReadService _hotelReadService;
    private readonly IHotelCommerceService _hotelCommerceService;
    private readonly IRoomEntryService _roomEntryService;
    private readonly IRoomInteractionService _roomInteractionService;
    private readonly IRoomRuntimeSnapshotService _roomRuntimeSnapshotService;
    private readonly IGameRuntimeService _gameRuntimeService;

    public ProtocolCommandExecutionService(
        ProtocolCommandRegistry protocolCommandRegistry,
        ISessionStore sessionStore,
        IHotelReadService hotelReadService,
        IHotelCommerceService hotelCommerceService,
        IRoomEntryService roomEntryService,
        IRoomInteractionService roomInteractionService,
        IRoomRuntimeSnapshotService roomRuntimeSnapshotService,
        IGameRuntimeService gameRuntimeService)
    {
        _protocolCommandRegistry = protocolCommandRegistry;
        _sessionStore = sessionStore;
        _hotelReadService = hotelReadService;
        _hotelCommerceService = hotelCommerceService;
        _roomEntryService = roomEntryService;
        _roomInteractionService = roomInteractionService;
        _roomRuntimeSnapshotService = roomRuntimeSnapshotService;
        _gameRuntimeService = gameRuntimeService;
    }

    public async ValueTask<ProtocolCommandExecutionResult> ExecuteAsync(
        string commandName,
        IReadOnlyDictionary<string, JsonElement>? arguments,
        SessionTicket? boundSession,
        string? remoteAddress,
        string? presentedTicket,
        CancellationToken cancellationToken)
    {
        if (!_protocolCommandRegistry.TryGet(commandName, out ProtocolCommandDefinition? command) || command is null)
        {
            return new ProtocolCommandExecutionResult(
                StatusCodes.Status400BadRequest,
                new { error = "unknown_protocol_command", commandName },
                boundSession);
        }

        IReadOnlyDictionary<string, JsonElement> resolvedArguments =
            arguments ?? new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

        return command.Name switch
        {
            "session.initialize_crypto" => new ProtocolCommandExecutionResult(
                StatusCodes.Status200OK,
                new
                {
                    command = command.Name,
                    packetName = command.PacketName,
                    flow = command.Flow,
                    protocolFamily = _protocolCommandRegistry.Family,
                    protocolRevision = _protocolCommandRegistry.Revision,
                    crypto = "session-bootstrap",
                    transport = "realtime"
                },
                boundSession),
            "session.authenticate_with_ticket" => await ExecuteTicketAuthenticationAsync(
                boundSession,
                remoteAddress,
                presentedTicket,
                cancellationToken),
            "identity.get_self" => await ExecuteGetSelfAsync(
                boundSession,
                remoteAddress,
                cancellationToken),
            "rooms.enter_room" => await ExecuteRoomEntryAsync(
                resolvedArguments,
                boundSession,
                remoteAddress,
                cancellationToken),
            "rooms.get_runtime" => await ExecuteRoomRuntimeSnapshotAsync(
                resolvedArguments,
                boundSession,
                remoteAddress,
                cancellationToken),
            "rooms.move_avatar" => await ExecuteMoveAsync(
                resolvedArguments,
                boundSession,
                remoteAddress,
                cancellationToken),
            "rooms.chat" => await ExecuteChatAsync(
                resolvedArguments,
                boundSession,
                remoteAddress,
                cancellationToken),
            "inventory.get_self" => await ExecuteInventoryAsync(
                boundSession,
                remoteAddress,
                cancellationToken),
            "catalog.purchase" => await ExecuteCatalogPurchaseAsync(
                resolvedArguments,
                boundSession,
                remoteAddress,
                cancellationToken),
            "games.list_sessions" => await ExecuteGameSessionsAsync(
                boundSession,
                remoteAddress,
                cancellationToken),
            "games.get_session" => await ExecuteGameSessionAsync(
                resolvedArguments,
                boundSession,
                remoteAddress,
                cancellationToken),
            _ => new ProtocolCommandExecutionResult(
                StatusCodes.Status400BadRequest,
                new { error = "unsupported_protocol_command", commandName = command.Name },
                boundSession)
        };
    }

    private async ValueTask<ProtocolCommandExecutionResult> ExecuteTicketAuthenticationAsync(
        SessionTicket? boundSession,
        string? remoteAddress,
        string? presentedTicket,
        CancellationToken cancellationToken)
    {
        string? ticket = !string.IsNullOrWhiteSpace(presentedTicket)
            ? presentedTicket
            : boundSession?.Ticket;

        if (string.IsNullOrWhiteSpace(ticket))
        {
            return new ProtocolCommandExecutionResult(
                StatusCodes.Status401Unauthorized,
                new { error = "session_ticket_required" },
                boundSession);
        }

        SessionTicket? session = await _sessionStore.TouchAsync(ticket, remoteAddress, cancellationToken);
        if (session is null || session.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return new ProtocolCommandExecutionResult(
                StatusCodes.Status401Unauthorized,
                new { error = "invalid_or_expired_session" },
                null);
        }

        return new ProtocolCommandExecutionResult(
            StatusCodes.Status200OK,
            new
            {
                authenticated = true,
                session
            },
            session);
    }

    private async ValueTask<ProtocolCommandExecutionResult> ExecuteGetSelfAsync(
        SessionTicket? boundSession,
        string? remoteAddress,
        CancellationToken cancellationToken)
    {
        SessionTicket? session = await RequireActiveSessionAsync(boundSession, remoteAddress, cancellationToken);
        if (session is null)
        {
            return new ProtocolCommandExecutionResult(
                StatusCodes.Status401Unauthorized,
                new { error = "session_required" },
                null);
        }

        CharacterHotelSnapshot? snapshot = await _hotelReadService.GetCharacterSnapshotAsync(
            new CharacterId(session.CharacterId),
            cancellationToken);

        return snapshot is null
            ? new ProtocolCommandExecutionResult(StatusCodes.Status404NotFound, new { error = "character_not_found" }, session)
            : new ProtocolCommandExecutionResult(StatusCodes.Status200OK, snapshot, session);
    }

    private async ValueTask<ProtocolCommandExecutionResult> ExecuteRoomEntryAsync(
        IReadOnlyDictionary<string, JsonElement> arguments,
        SessionTicket? boundSession,
        string? remoteAddress,
        CancellationToken cancellationToken)
    {
        SessionTicket? session = await RequireActiveSessionAsync(boundSession, remoteAddress, cancellationToken);
        if (session is null)
        {
            return new ProtocolCommandExecutionResult(
                StatusCodes.Status401Unauthorized,
                new { error = "session_required" },
                null);
        }

        if (!TryReadInt64(arguments, "roomId", out long roomId))
        {
            return new ProtocolCommandExecutionResult(
                StatusCodes.Status400BadRequest,
                new { error = "roomId_required" },
                session);
        }

        RoomEntryResult result = await _roomEntryService.EnterAsync(
            new RoomEntryRequest(
                new CharacterId(session.CharacterId),
                new RoomId(roomId),
                TryReadString(arguments, "password"),
                TryReadBoolean(arguments, "spectatorMode")),
            cancellationToken);

        return new ProtocolCommandExecutionResult(
            result.Succeeded ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest,
            result,
            session);
    }

    private async ValueTask<ProtocolCommandExecutionResult> ExecuteMoveAsync(
        IReadOnlyDictionary<string, JsonElement> arguments,
        SessionTicket? boundSession,
        string? remoteAddress,
        CancellationToken cancellationToken)
    {
        SessionTicket? session = await RequireActiveSessionAsync(boundSession, remoteAddress, cancellationToken);
        if (session is null)
        {
            return new ProtocolCommandExecutionResult(
                StatusCodes.Status401Unauthorized,
                new { error = "session_required" },
                null);
        }

        if (!TryReadInt64(arguments, "roomId", out long roomId) ||
            !TryReadInt32(arguments, "destinationX", out int destinationX) ||
            !TryReadInt32(arguments, "destinationY", out int destinationY))
        {
            return new ProtocolCommandExecutionResult(
                StatusCodes.Status400BadRequest,
                new { error = "roomId_destinationX_destinationY_required" },
                session);
        }

        RoomActorMovementResult result = await _roomInteractionService.MoveActorAsync(
            new RoomActorMovementRequest(
                new CharacterId(session.CharacterId),
                new RoomId(roomId),
                destinationX,
                destinationY),
            cancellationToken);

        return new ProtocolCommandExecutionResult(
            result.Succeeded ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest,
            result,
            session);
    }

    private async ValueTask<ProtocolCommandExecutionResult> ExecuteRoomRuntimeSnapshotAsync(
        IReadOnlyDictionary<string, JsonElement> arguments,
        SessionTicket? boundSession,
        string? remoteAddress,
        CancellationToken cancellationToken)
    {
        SessionTicket? session = await RequireActiveSessionAsync(boundSession, remoteAddress, cancellationToken);
        if (session is null)
        {
            return new ProtocolCommandExecutionResult(
                StatusCodes.Status401Unauthorized,
                new { error = "session_required" },
                null);
        }

        if (!TryReadInt64(arguments, "roomId", out long roomId))
        {
            return new ProtocolCommandExecutionResult(
                StatusCodes.Status400BadRequest,
                new { error = "roomId_required" },
                session);
        }

        RoomRuntimeSnapshot? snapshot = await _roomRuntimeSnapshotService.BuildAsync(new RoomId(roomId), cancellationToken);
        return snapshot is null
            ? new ProtocolCommandExecutionResult(StatusCodes.Status404NotFound, new { error = "room_runtime_not_found" }, session)
            : new ProtocolCommandExecutionResult(StatusCodes.Status200OK, snapshot, session);
    }

    private async ValueTask<ProtocolCommandExecutionResult> ExecuteChatAsync(
        IReadOnlyDictionary<string, JsonElement> arguments,
        SessionTicket? boundSession,
        string? remoteAddress,
        CancellationToken cancellationToken)
    {
        SessionTicket? session = await RequireActiveSessionAsync(boundSession, remoteAddress, cancellationToken);
        if (session is null)
        {
            return new ProtocolCommandExecutionResult(
                StatusCodes.Status401Unauthorized,
                new { error = "session_required" },
                null);
        }

        string? message = TryReadString(arguments, "message");
        if (!TryReadInt64(arguments, "roomId", out long roomId) || string.IsNullOrWhiteSpace(message))
        {
            return new ProtocolCommandExecutionResult(
                StatusCodes.Status400BadRequest,
                new { error = "roomId_message_required" },
                session);
        }

        RoomChatResult result = await _roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(session.CharacterId), new RoomId(roomId), message),
            cancellationToken);

        return new ProtocolCommandExecutionResult(
            result.Succeeded ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest,
            result,
            session);
    }

    private async ValueTask<ProtocolCommandExecutionResult> ExecuteInventoryAsync(
        SessionTicket? boundSession,
        string? remoteAddress,
        CancellationToken cancellationToken)
    {
        SessionTicket? session = await RequireActiveSessionAsync(boundSession, remoteAddress, cancellationToken);
        if (session is null)
        {
            return new ProtocolCommandExecutionResult(
                StatusCodes.Status401Unauthorized,
                new { error = "session_required" },
                null);
        }

        InventorySnapshot snapshot = await _hotelCommerceService.GetInventoryAsync(
            new CharacterId(session.CharacterId),
            cancellationToken);

        return new ProtocolCommandExecutionResult(StatusCodes.Status200OK, snapshot, session);
    }

    private async ValueTask<ProtocolCommandExecutionResult> ExecuteCatalogPurchaseAsync(
        IReadOnlyDictionary<string, JsonElement> arguments,
        SessionTicket? boundSession,
        string? remoteAddress,
        CancellationToken cancellationToken)
    {
        SessionTicket? session = await RequireActiveSessionAsync(boundSession, remoteAddress, cancellationToken);
        if (session is null)
        {
            return new ProtocolCommandExecutionResult(
                StatusCodes.Status401Unauthorized,
                new { error = "session_required" },
                null);
        }

        if (!TryReadInt64(arguments, "catalogOfferId", out long catalogOfferId))
        {
            return new ProtocolCommandExecutionResult(
                StatusCodes.Status400BadRequest,
                new { error = "catalogOfferId_required" },
                session);
        }

        CatalogPurchaseResult result = await _hotelCommerceService.PurchaseAsync(
            new CatalogPurchaseRequest(new CharacterId(session.CharacterId), new CatalogOfferId(catalogOfferId)),
            cancellationToken);

        return new ProtocolCommandExecutionResult(
            result.Succeeded ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest,
            result,
            session);
    }

    private async ValueTask<ProtocolCommandExecutionResult> ExecuteGameSessionsAsync(
        SessionTicket? boundSession,
        string? remoteAddress,
        CancellationToken cancellationToken)
    {
        SessionTicket? session = await RequireActiveSessionAsync(boundSession, remoteAddress, cancellationToken);
        if (session is null)
        {
            return new ProtocolCommandExecutionResult(
                StatusCodes.Status401Unauthorized,
                new { error = "session_required" },
                null);
        }

        IReadOnlyList<GameSessionState> sessions = await _gameRuntimeService.GetActiveSessionsAsync(cancellationToken);
        return new ProtocolCommandExecutionResult(StatusCodes.Status200OK, new GameRuntimeCatalogSnapshot(sessions), session);
    }

    private async ValueTask<ProtocolCommandExecutionResult> ExecuteGameSessionAsync(
        IReadOnlyDictionary<string, JsonElement> arguments,
        SessionTicket? boundSession,
        string? remoteAddress,
        CancellationToken cancellationToken)
    {
        SessionTicket? session = await RequireActiveSessionAsync(boundSession, remoteAddress, cancellationToken);
        if (session is null)
        {
            return new ProtocolCommandExecutionResult(
                StatusCodes.Status401Unauthorized,
                new { error = "session_required" },
                null);
        }

        string? sessionKey = TryReadString(arguments, "sessionKey");
        if (string.IsNullOrWhiteSpace(sessionKey))
        {
            return new ProtocolCommandExecutionResult(
                StatusCodes.Status400BadRequest,
                new { error = "sessionKey_required" },
                session);
        }

        GameSessionState? snapshot = await _gameRuntimeService.GetSessionAsync(sessionKey, cancellationToken);
        return snapshot is null
            ? new ProtocolCommandExecutionResult(StatusCodes.Status404NotFound, new { error = "game_session_not_found" }, session)
            : new ProtocolCommandExecutionResult(StatusCodes.Status200OK, snapshot, session);
    }

    private async ValueTask<SessionTicket?> RequireActiveSessionAsync(
        SessionTicket? boundSession,
        string? remoteAddress,
        CancellationToken cancellationToken)
    {
        if (boundSession is null || boundSession.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return null;
        }

        SessionTicket? refreshed = await _sessionStore.TouchAsync(boundSession.Ticket, remoteAddress, cancellationToken);
        return refreshed is not null && refreshed.ExpiresAtUtc > DateTime.UtcNow ? refreshed : null;
    }

    public static bool TryReadInt64(
        IReadOnlyDictionary<string, JsonElement> arguments,
        string key,
        out long value)
    {
        value = default;
        return arguments.TryGetValue(key, out JsonElement argument) && argument.TryGetInt64(out value);
    }

    public static bool TryReadInt32(
        IReadOnlyDictionary<string, JsonElement> arguments,
        string key,
        out int value)
    {
        value = default;
        return arguments.TryGetValue(key, out JsonElement argument) && argument.TryGetInt32(out value);
    }

    public static string? TryReadString(
        IReadOnlyDictionary<string, JsonElement> arguments,
        string key)
    {
        if (!arguments.TryGetValue(key, out JsonElement argument))
        {
            return null;
        }

        return argument.ValueKind == JsonValueKind.String
            ? argument.GetString()
            : argument.ToString();
    }

    public static bool TryReadBoolean(
        IReadOnlyDictionary<string, JsonElement> arguments,
        string key)
    {
        return arguments.TryGetValue(key, out JsonElement argument) &&
               argument.ValueKind == JsonValueKind.True
            ? true
            : arguments.TryGetValue(key, out argument) &&
              argument.ValueKind == JsonValueKind.False
                ? false
                : arguments.TryGetValue(key, out argument) &&
                  argument.ValueKind == JsonValueKind.String &&
                  bool.TryParse(argument.GetString(), out bool parsed) &&
                  parsed;
    }
}
