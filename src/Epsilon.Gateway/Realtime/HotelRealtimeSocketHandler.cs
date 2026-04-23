using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Epsilon.Auth;
using Epsilon.CoreGame;
using Microsoft.Extensions.Options;

namespace Epsilon.Gateway;

public sealed class HotelRealtimeSocketHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly GatewayRuntimeOptions _gatewayRuntimeOptions;
    private readonly ProtocolCommandExecutionService _protocolCommandExecutionService;
    private readonly IRoomRuntimeRepository _roomRuntimeRepository;
    private readonly IRoomRuntimeCoordinator _roomRuntimeCoordinator;
    private readonly IRoomRuntimeSnapshotService _roomRuntimeSnapshotService;
    private readonly IRealtimeConnectionMonitor _realtimeConnectionMonitor;
    private readonly ILogger<HotelRealtimeSocketHandler> _logger;

    public HotelRealtimeSocketHandler(
        IOptions<GatewayRuntimeOptions> gatewayRuntimeOptions,
        ProtocolCommandExecutionService protocolCommandExecutionService,
        IRoomRuntimeRepository roomRuntimeRepository,
        IRoomRuntimeCoordinator roomRuntimeCoordinator,
        IRoomRuntimeSnapshotService roomRuntimeSnapshotService,
        IRealtimeConnectionMonitor realtimeConnectionMonitor,
        ILogger<HotelRealtimeSocketHandler> logger)
    {
        _gatewayRuntimeOptions = gatewayRuntimeOptions.Value;
        _protocolCommandExecutionService = protocolCommandExecutionService;
        _roomRuntimeRepository = roomRuntimeRepository;
        _roomRuntimeCoordinator = roomRuntimeCoordinator;
        _roomRuntimeSnapshotService = roomRuntimeSnapshotService;
        _realtimeConnectionMonitor = realtimeConnectionMonitor;
        _logger = logger;
    }

    public async Task HandleAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        if (!httpContext.WebSockets.IsWebSocketRequest)
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsJsonAsync(new { error = "websocket_upgrade_required" }, cancellationToken);
            return;
        }

        if (!IsRealtimeTransportAllowed(httpContext))
        {
            httpContext.Response.StatusCode = StatusCodes.Status426UpgradeRequired;
            await httpContext.Response.WriteAsJsonAsync(new { error = "tls_required_for_realtime" }, cancellationToken);
            return;
        }

        using WebSocket socket = await httpContext.WebSockets.AcceptWebSocketAsync();
        _realtimeConnectionMonitor.RecordAcceptedConnection();

        string remoteAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        SessionTicket? boundSession = null;

        await SendResponseAsync(
            socket,
            new RealtimeCommandResponse(
                Id: null,
                Command: "system.hello",
                Ok: true,
                StatusCode: StatusCodes.Status200OK,
                Payload: new
                {
                    transport = httpContext.Request.IsHttps ? "wss" : "ws",
                    realtimePath = _gatewayRuntimeOptions.RealtimePath,
                    heartbeatSeconds = _gatewayRuntimeOptions.RealtimeKeepAliveSeconds,
                    sessionBound = false
                }),
            cancellationToken);

        try
        {
            while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                string? text = await ReceiveTextMessageAsync(socket, cancellationToken);
                if (text is null)
                {
                    break;
                }

                if (string.Equals(text, "ping", StringComparison.OrdinalIgnoreCase))
                {
                    await SendRawTextAsync(socket, "pong", cancellationToken);
                    continue;
                }

                RealtimeCommandRequest? request;
                try
                {
                    request = JsonSerializer.Deserialize<RealtimeCommandRequest>(text, JsonOptions);
                }
                catch (JsonException exception)
                {
                    _logger.LogWarning(exception, "Rejected malformed realtime payload.");
                    await SendResponseAsync(
                        socket,
                        new RealtimeCommandResponse(
                            Id: null,
                            Command: "system.error",
                            Ok: false,
                            StatusCode: StatusCodes.Status400BadRequest,
                            Payload: new { error = "malformed_realtime_payload" }),
                        cancellationToken);
                    continue;
                }

                if (request is null || string.IsNullOrWhiteSpace(request.Command))
                {
                    await SendResponseAsync(
                        socket,
                        new RealtimeCommandResponse(
                            request?.Id,
                            request?.Command ?? "unknown",
                            false,
                            StatusCodes.Status400BadRequest,
                            new { error = "command_required" }),
                        cancellationToken);
                    continue;
                }

                ProtocolCommandExecutionResult result = await _protocolCommandExecutionService.ExecuteAsync(
                    request.Command,
                    request.Arguments,
                    boundSession,
                    remoteAddress,
                    request.Ticket,
                    cancellationToken);

                boundSession = result.BoundSession ?? boundSession;

                await SendResponseAsync(
                    socket,
                    new RealtimeCommandResponse(
                        request.Id,
                        request.Command,
                        result.StatusCode is >= 200 and < 300,
                        result.StatusCode,
                        result.Payload),
                    cancellationToken);

                await TrySendDerivedEventsAsync(
                    socket,
                    request.Command,
                    request.Arguments,
                    result,
                    cancellationToken);
            }
        }
        finally
        {
            _realtimeConnectionMonitor.RecordClosedConnection();

            if (boundSession is not null)
            {
                IReadOnlyList<RoomId> removedRoomIds = await _roomRuntimeRepository.RemoveActorFromAllRoomsAsync(
                    boundSession.CharacterId,
                    cancellationToken);

                foreach (RoomId roomId in removedRoomIds)
                {
                    await _roomRuntimeCoordinator.SignalMutationAsync(
                        roomId,
                        RoomRuntimeMutationKind.ActorPresenceChanged,
                        cancellationToken);
                }
            }

            if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "session_closed", CancellationToken.None);
            }
        }
    }

    private async Task TrySendDerivedEventsAsync(
        WebSocket socket,
        string commandName,
        IReadOnlyDictionary<string, JsonElement>? arguments,
        ProtocolCommandExecutionResult result,
        CancellationToken cancellationToken)
    {
        if (result.StatusCode is < 200 or >= 300 || arguments is null)
        {
            return;
        }

        if (commandName is not ("rooms.enter_room" or "rooms.move_avatar" or "rooms.chat"))
        {
            return;
        }

        if (!ProtocolCommandExecutionService.TryReadInt64(arguments, "roomId", out long roomId))
        {
            return;
        }

        RoomRuntimeSnapshot? snapshot = await _roomRuntimeSnapshotService.BuildAsync(new RoomId(roomId), cancellationToken);
        if (snapshot is null)
        {
            return;
        }

        await SendEventAsync(
            socket,
            new RealtimeServerEvent("room.runtime_snapshot", snapshot),
            cancellationToken);
    }

    private bool IsRealtimeTransportAllowed(HttpContext httpContext)
    {
        if (!_gatewayRuntimeOptions.RequireTlsForRealtime || httpContext.Request.IsHttps)
        {
            return true;
        }

        if (!_gatewayRuntimeOptions.AllowInsecureLoopbackRealtime)
        {
            return false;
        }

        string host = httpContext.Request.Host.Host;
        IPAddress? remoteIp = httpContext.Connection.RemoteIpAddress;

        return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
               IPAddress.TryParse(host, out IPAddress? hostAddress) && IPAddress.IsLoopback(hostAddress) ||
               remoteIp is not null && IPAddress.IsLoopback(remoteIp);
    }

    private static async Task<string?> ReceiveTextMessageAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        ArraySegment<byte> buffer = new(new byte[8192]);
        using MemoryStream stream = new();

        while (true)
        {
            WebSocketReceiveResult result = await socket.ReceiveAsync(buffer, cancellationToken);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                return null;
            }

            if (result.MessageType != WebSocketMessageType.Text)
            {
                continue;
            }

            stream.Write(buffer.Array!, buffer.Offset, result.Count);

            if (result.EndOfMessage)
            {
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }
    }

    private static Task SendResponseAsync(
        WebSocket socket,
        RealtimeCommandResponse response,
        CancellationToken cancellationToken)
    {
        string payload = JsonSerializer.Serialize(response, JsonOptions);
        return SendRawTextAsync(socket, payload, cancellationToken);
    }

    private static Task SendEventAsync(
        WebSocket socket,
        RealtimeServerEvent response,
        CancellationToken cancellationToken)
    {
        string payload = JsonSerializer.Serialize(response, JsonOptions);
        return SendRawTextAsync(socket, payload, cancellationToken);
    }

    private static Task SendRawTextAsync(
        WebSocket socket,
        string payload,
        CancellationToken cancellationToken)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(payload);
        return socket.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            true,
            cancellationToken);
    }
}
