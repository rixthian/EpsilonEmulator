using System.Collections.Concurrent;
using System.Globalization;

namespace Epsilon.CoreGame;

public sealed class RoomInteractionService : IRoomInteractionService
{
    private readonly IHotelReadService _hotelReadService;
    private readonly IRoomRuntimeRepository _roomRuntimeRepository;
    private readonly IChatCommandRepository _chatCommandRepository;
    private readonly IRoleAccessRepository _roleAccessRepository;
    // Flood control is tracked per room and character so the room runtime can
    // reject bursts before they become packet spam or command spam.
    private readonly ConcurrentDictionary<string, Queue<DateTime>> _chatWindows = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, object> _chatWindowLocks = new(StringComparer.Ordinal);

    public RoomInteractionService(
        IHotelReadService hotelReadService,
        IRoomRuntimeRepository roomRuntimeRepository,
        IChatCommandRepository chatCommandRepository,
        IRoleAccessRepository roleAccessRepository)
    {
        _hotelReadService = hotelReadService;
        _roomRuntimeRepository = roomRuntimeRepository;
        _chatCommandRepository = chatCommandRepository;
        _roleAccessRepository = roleAccessRepository;
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

        if (await IsTileBlockedAsync(room, actor, request.DestinationX, request.DestinationY, cancellationToken))
        {
            return new RoomActorMovementResult(false, "Destination tile is blocked by another actor or furniture.", actor);
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

        if (!TryRegisterChatWindow(request, policy, out string? floodFailure))
        {
            return new RoomChatResult(false, floodFailure ?? "Flood control is active.", false, null, null);
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

        // The command catalog alone is not trusted as the final authority.
        // Commands that affect room or hotel state are re-checked against
        // capability assignments here to avoid client-driven privilege abuse.
        string? requiredCapability = GetRequiredCapability(command.CommandKey);
        if (requiredCapability is not null &&
            !await HasCapabilityAsync(request.CharacterId, requiredCapability, cancellationToken))
        {
            return new RoomChatResult(
                false,
                $"Command '{command.CommandKey}' requires capability '{requiredCapability}'.",
                true,
                command.CommandKey,
                null);
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
        if (!TryNormalizeSignValue(signValue, out string resolvedSign, out string? failure))
        {
            return failure ?? "Invalid sign value.";
        }

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
        if (!TryNormalizeCarryItemTypeId(itemCode, out int resolvedItemTypeId, out string? failure))
        {
            return failure ?? "Invalid carry item.";
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

    private async ValueTask<bool> IsTileBlockedAsync(
        RoomHotelSnapshot room,
        RoomActorState actor,
        int x,
        int y,
        CancellationToken cancellationToken)
    {
        // The current runtime slice only performs basic blocking checks.
        // This prevents obvious clipping through actors and non-walkable furni
        // until a fuller pathing and occupancy model is introduced.
        IReadOnlyList<RoomActorState> actors = await _roomRuntimeRepository.GetActorsByRoomIdAsync(
            room.Room.RoomId,
            cancellationToken);

        bool occupiedByOtherActor = actors.Any(other =>
            other.ActorId != actor.ActorId &&
            other.Position.X == x &&
            other.Position.Y == y);

        if (occupiedByOtherActor)
        {
            return true;
        }

        return room.Items.Any(item =>
            item.Item.Placement.FloorPosition is { } floorPosition &&
            floorPosition.X == x &&
            floorPosition.Y == y &&
            !(item.Definition?.IsWalkable ?? false));
    }

    private bool TryRegisterChatWindow(
        RoomChatRequest request,
        RoomChatPolicySnapshot policy,
        out string? failure)
    {
        if (policy.FloodWindowSeconds <= 0 || policy.MaxMessagesPerWindow <= 0)
        {
            failure = null;
            return true;
        }

        string windowKey = $"{request.RoomId.Value}:{request.CharacterId.Value}";
        object syncRoot = _chatWindowLocks.GetOrAdd(windowKey, static _ => new object());
        Queue<DateTime> window = _chatWindows.GetOrAdd(windowKey, static _ => new Queue<DateTime>());
        DateTime utcNow = DateTime.UtcNow;
        DateTime cutoff = utcNow.AddSeconds(-policy.FloodWindowSeconds);

        // The queue contains only timestamps inside the active anti-flood
        // window, which keeps enforcement deterministic and cheap.
        lock (syncRoot)
        {
            while (window.Count > 0 && window.Peek() < cutoff)
            {
                window.Dequeue();
            }

            if (window.Count >= policy.MaxMessagesPerWindow)
            {
                failure = $"Flood control is active. Max {policy.MaxMessagesPerWindow} messages every {policy.FloodWindowSeconds} seconds.";
                return false;
            }

            window.Enqueue(utcNow);
        }

        failure = null;
        return true;
    }

    private async ValueTask<bool> HasCapabilityAsync(
        CharacterId characterId,
        string capabilityKey,
        CancellationToken cancellationToken)
    {
        // Role assignments are resolved dynamically so moderation-sensitive
        // commands react to role changes without relying on stale client state.
        IReadOnlyList<StaffRoleAssignment> assignments =
            await _roleAccessRepository.GetAssignmentsByCharacterIdAsync(characterId, cancellationToken);
        if (assignments.Count == 0)
        {
            return false;
        }

        HashSet<string> assignedRoles = assignments
            .Select(assignment => assignment.RoleKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        IReadOnlyList<StaffRoleDefinition> roleDefinitions =
            await _roleAccessRepository.GetRoleDefinitionsAsync(cancellationToken);

        return roleDefinitions.Any(role =>
            assignedRoles.Contains(role.RoleKey) &&
            role.CapabilityKeys.Any(candidate => string.Equals(candidate, capabilityKey, StringComparison.OrdinalIgnoreCase)));
    }

    private static string? GetRequiredCapability(string commandKey)
    {
        return commandKey.ToLowerInvariant() switch
        {
            "roommute" => "room.mute",
            "pickall" => "room.pick_all",
            "ha" => "hotel.alert",
            _ => null
        };
    }

    private static bool TryNormalizeSignValue(
        string? rawValue,
        out string normalizedValue,
        out string? failure)
    {
        normalizedValue = "0";
        failure = null;

        // Sign values are intentionally narrow because these status payloads
        // eventually flow into client rendering and packet serialization.
        string candidate = string.IsNullOrWhiteSpace(rawValue) ? "0" : rawValue.Trim();
        if (!int.TryParse(candidate, NumberStyles.None, CultureInfo.InvariantCulture, out int signNumber) ||
            signNumber < 0 ||
            signNumber > 99)
        {
            failure = "Sign value must be an integer between 0 and 99.";
            return false;
        }

        normalizedValue = signNumber.ToString(CultureInfo.InvariantCulture);
        return true;
    }

    private static bool TryNormalizeCarryItemTypeId(
        string? rawValue,
        out int normalizedValue,
        out string? failure)
    {
        normalizedValue = 0;
        failure = null;

        // Carry item ids are restricted to a small numeric range until the
        // content pipeline owns a canonical hand-item catalog.
        string candidate = string.IsNullOrWhiteSpace(rawValue) ? "0" : rawValue.Trim();
        if (!int.TryParse(candidate, NumberStyles.None, CultureInfo.InvariantCulture, out int itemTypeId) ||
            itemTypeId < 0 ||
            itemTypeId > 999)
        {
            failure = "Carry item type must be an integer between 0 and 999.";
            return false;
        }

        normalizedValue = itemTypeId;
        return true;
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
