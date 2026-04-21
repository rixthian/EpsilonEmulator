namespace Epsilon.CoreGame;

public sealed class RoomInteractionService : IRoomInteractionService
{
    private readonly IHotelReadService _hotelReadService;
    private readonly IRoomRuntimeRepository _roomRuntimeRepository;
    private readonly IChatCommandRepository _chatCommandRepository;

    public RoomInteractionService(
        IHotelReadService hotelReadService,
        IRoomRuntimeRepository roomRuntimeRepository,
        IChatCommandRepository chatCommandRepository)
    {
        _hotelReadService = hotelReadService;
        _roomRuntimeRepository = roomRuntimeRepository;
        _chatCommandRepository = chatCommandRepository;
    }

    public async ValueTask<RoomActorMovementResult> MoveActorAsync(
        RoomActorMovementRequest request,
        CancellationToken cancellationToken = default)
    {
        RoomHotelSnapshot? room = await _hotelReadService.GetRoomSnapshotAsync(request.RoomId, cancellationToken);
        if (room is null || room.Layout is null)
        {
            return new RoomActorMovementResult(false, "Room or room layout could not be resolved.", null);
        }

        RoomActorState? actor = await _roomRuntimeRepository.GetActorByIdAsync(
            request.RoomId,
            request.CharacterId.Value,
            cancellationToken);

        if (actor is null)
        {
            return new RoomActorMovementResult(false, "Actor is not present in the room runtime.", null);
        }

        if (!IsWalkable(room.Layout, request.DestinationX, request.DestinationY, out double targetHeight))
        {
            return new RoomActorMovementResult(false, "Destination tile is not walkable.", actor);
        }

        RoomActorState updatedActor = actor with
        {
            Position = new RoomCoordinate(request.DestinationX, request.DestinationY, targetHeight),
            Goal = new MovementGoal(request.DestinationX, request.DestinationY),
            IsWalking = true,
            StatusEntries = BuildMovementStatusEntries(request.DestinationX, request.DestinationY, targetHeight)
        };

        await _roomRuntimeRepository.StoreActorStateAsync(request.RoomId, updatedActor, cancellationToken);

        return new RoomActorMovementResult(
            true,
            $"Actor moved to {request.DestinationX},{request.DestinationY}.",
            updatedActor);
    }

    public async ValueTask<RoomChatResult> SendChatAsync(
        RoomChatRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return new RoomChatResult(false, "Message cannot be empty.", false, null, null);
        }

        RoomActorState? actor = await _roomRuntimeRepository.GetActorByIdAsync(
            request.RoomId,
            request.CharacterId.Value,
            cancellationToken);

        if (actor is null)
        {
            return new RoomChatResult(false, "Actor is not present in the room runtime.", false, null, null);
        }

        RoomChatPolicySnapshot policy =
            await _roomRuntimeRepository.GetChatPolicyByRoomIdAsync(request.RoomId, cancellationToken)
            ?? new RoomChatPolicySnapshot(false, false, 100, 4, 4);

        string trimmedMessage = request.Message.Trim();

        if (trimmedMessage.Length > policy.MaxMessageLength)
        {
            return new RoomChatResult(false, $"Message exceeds the maximum length of {policy.MaxMessageLength}.", false, null, null);
        }

        if (trimmedMessage.StartsWith(':'))
        {
            return await ExecuteCommandAsync(request, actor, trimmedMessage, cancellationToken);
        }

        if (policy.IsMuted)
        {
            return new RoomChatResult(false, "Room chat is currently muted.", false, null, null);
        }

        RoomChatMessage chatMessage = await _roomRuntimeRepository.AppendChatMessageAsync(
            request.RoomId,
            actor.ActorId,
            actor.DisplayName,
            trimmedMessage,
            RoomChatMessageKind.User,
            cancellationToken);

        return new RoomChatResult(true, "Chat message sent.", false, null, chatMessage);
    }

    private async ValueTask<RoomChatResult> ExecuteCommandAsync(
        RoomChatRequest request,
        RoomActorState actor,
        string message,
        CancellationToken cancellationToken)
    {
        string[] parts = message[1..]
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
        {
            return new RoomChatResult(false, "Command input is empty.", true, null, null);
        }

        IReadOnlyList<ChatCommandDefinition> availableCommands =
            await _chatCommandRepository.GetAvailableByCharacterIdAsync(request.CharacterId, cancellationToken);
        ChatCommandDefinition? command = availableCommands.FirstOrDefault(candidate =>
            string.Equals(candidate.CommandKey, parts[0], StringComparison.OrdinalIgnoreCase) ||
            candidate.Aliases.Any(alias => string.Equals(alias, parts[0], StringComparison.OrdinalIgnoreCase)));

        if (command is null)
        {
            return new RoomChatResult(false, $"Unknown command '{parts[0]}'.", true, null, null);
        }

        string response = command.CommandKey switch
        {
            "help" => BuildHelpMessage(availableCommands),
            "coords" => $"Coordinates: {actor.Position.X},{actor.Position.Y},{actor.Position.Z:0.##}",
            "userinfo" => $"Actor {actor.DisplayName} at {actor.Position.X},{actor.Position.Y},{actor.Position.Z:0.##} walking={actor.IsWalking} typing={actor.IsTyping}.",
            "sign" => await SetSignAsync(request.RoomId, actor, parts.Skip(1).FirstOrDefault(), cancellationToken),
            "carry" => await SetCarryItemAsync(request.RoomId, actor, parts.Skip(1).FirstOrDefault(), cancellationToken),
            "roommute" => await ToggleRoomMuteAsync(request.RoomId, cancellationToken),
            "pickall" => "Pick-all execution is acknowledged and awaits inventory wiring.",
            "ha" => "Hotel alert queued for broadcast pipeline.",
            _ => $"Command '{command.CommandKey}' executed."
        };

        RoomChatMessage responseMessage = await _roomRuntimeRepository.AppendChatMessageAsync(
            request.RoomId,
            actor.ActorId,
            actor.DisplayName,
            response,
            RoomChatMessageKind.CommandResponse,
            cancellationToken);

        return new RoomChatResult(true, response, true, command.CommandKey, responseMessage);
    }

    private async ValueTask<string> ToggleRoomMuteAsync(
        RoomId roomId,
        CancellationToken cancellationToken)
    {
        RoomChatPolicySnapshot currentPolicy =
            await _roomRuntimeRepository.GetChatPolicyByRoomIdAsync(roomId, cancellationToken)
            ?? new RoomChatPolicySnapshot(false, false, 100, 4, 4);

        RoomChatPolicySnapshot updatedPolicy = currentPolicy with
        {
            IsMuted = !currentPolicy.IsMuted
        };

        await _roomRuntimeRepository.StoreChatPolicyAsync(roomId, updatedPolicy, cancellationToken);
        return updatedPolicy.IsMuted ? "Room chat has been muted." : "Room chat has been unmuted.";
    }

    private async ValueTask<string> SetSignAsync(
        RoomId roomId,
        RoomActorState actor,
        string? signValue,
        CancellationToken cancellationToken)
    {
        string resolvedSign = string.IsNullOrWhiteSpace(signValue) ? "0" : signValue.Trim();
        IReadOnlyList<ActorStatusEntry> statusEntries = actor.StatusEntries
            .Where(entry => !string.Equals(entry.Key, "sign", StringComparison.OrdinalIgnoreCase))
            .Concat([new ActorStatusEntry("sign", resolvedSign)])
            .ToArray();

        RoomActorState updatedActor = actor with
        {
            StatusEntries = statusEntries
        };

        await _roomRuntimeRepository.StoreActorStateAsync(roomId, updatedActor, cancellationToken);
        return $"Sign status updated to {resolvedSign}.";
    }

    private async ValueTask<string> SetCarryItemAsync(
        RoomId roomId,
        RoomActorState actor,
        string? itemCode,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(itemCode, out int resolvedItemTypeId))
        {
            resolvedItemTypeId = 0;
        }

        CarryItemState? carryItem = resolvedItemTypeId > 0
            ? new CarryItemState(resolvedItemTypeId, $"HandItem:{resolvedItemTypeId}", DateTime.UtcNow.AddMinutes(5))
            : null;

        RoomActorState updatedActor = actor with
        {
            CarryItem = carryItem
        };

        await _roomRuntimeRepository.StoreActorStateAsync(roomId, updatedActor, cancellationToken);
        return carryItem is null
            ? "Carry item cleared."
            : $"Carry item set to {carryItem.ItemTypeId}.";
    }

    private static IReadOnlyList<ActorStatusEntry> BuildMovementStatusEntries(
        int x,
        int y,
        double z)
    {
        return
        [
            new ActorStatusEntry("mv", $"{x},{y},{z:0.##}")
        ];
    }

    private static bool IsWalkable(
        Rooms.RoomLayoutDefinition layout,
        int x,
        int y,
        out double height)
    {
        string[] rows = layout.Heightmap.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        height = 0;

        if (y < 0 || y >= rows.Length)
        {
            return false;
        }

        string row = rows[y];
        if (x < 0 || x >= row.Length)
        {
            return false;
        }

        char tile = row[x];
        if (tile == 'x' || tile == 'X')
        {
            return false;
        }

        if (char.IsDigit(tile))
        {
            height = tile - '0';
            return true;
        }

        if (tile is 'a' or 'b' or 'c' or 'd' or 'e' or 'f')
        {
            height = 10 + (tile - 'a');
            return true;
        }

        if (tile is 'A' or 'B' or 'C' or 'D' or 'E' or 'F')
        {
            height = 10 + (tile - 'A');
            return true;
        }

        if (tile == '-')
        {
            height = 0;
            return true;
        }

        return false;
    }

    private static string BuildHelpMessage(IReadOnlyList<ChatCommandDefinition> availableCommands)
    {
        string[] commandKeys = availableCommands
            .OrderBy(command => command.CommandKey, StringComparer.OrdinalIgnoreCase)
            .Select(command => $":{command.CommandKey}")
            .ToArray();

        return commandKeys.Length == 0
            ? "No commands are available."
            : $"Available commands: {string.Join(", ", commandKeys)}";
    }
}
