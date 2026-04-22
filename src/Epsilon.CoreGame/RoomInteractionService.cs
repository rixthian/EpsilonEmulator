using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using Epsilon.Content;
using Epsilon.Games;
using Epsilon.Rooms;

namespace Epsilon.CoreGame;

public sealed class RoomInteractionService : IRoomInteractionService
{
    private readonly IHotelReadService _hotelReadService;
    private readonly IRoomRuntimeRepository _roomRuntimeRepository;
    private readonly IChatCommandRepository _chatCommandRepository;
    private readonly ICatalogFeatureStateRepository _catalogFeatureStateRepository;
    private readonly ICatalogOfferRepository _catalogOfferRepository;
    private readonly IAccessControlService _accessControlService;
    private readonly IRoomRuntimeCoordinator _roomRuntimeCoordinator;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IRoomItemRepository _roomItemRepository;
    private readonly IInterfacePreferenceService _interfacePreferenceService;
    private readonly IGameRuntimeService _gameRuntimeService;
    private readonly IBattleBallLifecycleService _battleBallLifecycleService;
    private readonly ISnowStormLifecycleService _snowStormLifecycleService;
    private readonly IWobbleSquabbleLifecycleService _wobbleSquabbleLifecycleService;
    private readonly ICharacterProfileRepository _characterProfileRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly IModerationRepository _moderationRepository;
    private readonly IHotelOperationalState _hotelOperationalState;
    // Flood control is tracked per room and character so the room runtime can
    // reject bursts before they become packet spam or command spam.
    private readonly ConcurrentDictionary<string, Queue<DateTime>> _chatWindows = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, object> _chatWindowLocks = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, DateTime> _actorMutes = new(StringComparer.Ordinal);
    // Periodic sweep removes stale flood-window and mute entries so the
    // dictionaries don't grow without bound under heavy multiplayer load.
    private readonly Timer _cleanupTimer;

    public RoomInteractionService(
        IHotelReadService hotelReadService,
        IRoomRuntimeRepository roomRuntimeRepository,
        IChatCommandRepository chatCommandRepository,
        ICatalogFeatureStateRepository catalogFeatureStateRepository,
        ICatalogOfferRepository catalogOfferRepository,
        IAccessControlService accessControlService,
        IRoomRuntimeCoordinator roomRuntimeCoordinator,
        IInventoryRepository inventoryRepository,
        IRoomItemRepository roomItemRepository,
        IInterfacePreferenceService interfacePreferenceService,
        IGameRuntimeService gameRuntimeService,
        IBattleBallLifecycleService battleBallLifecycleService,
        ISnowStormLifecycleService snowStormLifecycleService,
        IWobbleSquabbleLifecycleService wobbleSquabbleLifecycleService,
        ICharacterProfileRepository characterProfileRepository,
        IWalletRepository walletRepository,
        IModerationRepository moderationRepository,
        IHotelOperationalState hotelOperationalState)
    {
        _hotelReadService = hotelReadService;
        _roomRuntimeRepository = roomRuntimeRepository;
        _chatCommandRepository = chatCommandRepository;
        _catalogFeatureStateRepository = catalogFeatureStateRepository;
        _catalogOfferRepository = catalogOfferRepository;
        _accessControlService = accessControlService;
        _roomRuntimeCoordinator = roomRuntimeCoordinator;
        _inventoryRepository = inventoryRepository;
        _roomItemRepository = roomItemRepository;
        _interfacePreferenceService = interfacePreferenceService;
        _gameRuntimeService = gameRuntimeService;
        _battleBallLifecycleService = battleBallLifecycleService;
        _snowStormLifecycleService = snowStormLifecycleService;
        _wobbleSquabbleLifecycleService = wobbleSquabbleLifecycleService;
        _characterProfileRepository = characterProfileRepository;
        _walletRepository = walletRepository;
        _moderationRepository = moderationRepository;
        _hotelOperationalState = hotelOperationalState;

        // Sweep expired flood-control windows and mutes every 60 seconds to
        // prevent unbounded memory growth under heavy concurrent load.
        _cleanupTimer = new Timer(
            static state => ((RoomInteractionService)state!).PruneExpiredEntries(),
            this,
            TimeSpan.FromSeconds(60),
            TimeSpan.FromSeconds(60));
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
        await _roomRuntimeCoordinator.SignalMutationAsync(
            request.RoomId,
            RoomRuntimeMutationKind.ActorStateChanged,
            cancellationToken);

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

        string trimmedMessage = NormalizeChatText(request.Message);

        if (trimmedMessage.Length > policy.MaxMessageLength)
        {
            return new RoomChatResult(false, $"Message exceeds the maximum length of {policy.MaxMessageLength}.", false, null, null);
        }

        if (TryGetActiveActorMuteUntil(request.RoomId, request.CharacterId, out DateTime mutedUntilUtc))
        {
            return new RoomChatResult(
                false,
                $"You are muted in this room until {mutedUntilUtc:O}.",
                false,
                null,
                null);
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
        await _roomRuntimeCoordinator.SignalMutationAsync(
            request.RoomId,
            RoomRuntimeMutationKind.ChatMessageAppended,
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
        string? requiredCapability = command.RequiredCapabilityKey;
        if (requiredCapability is not null &&
            !await _accessControlService.HasCapabilityAsync(request.CharacterId, requiredCapability, cancellationToken))
        {
            return new RoomChatResult(
                false,
                $"Command '{command.CommandKey}' requires capability '{requiredCapability}'.",
                true,
                command.CommandKey,
                null);
        }

        string normalizedCommandKey = command.CommandKey.ToLowerInvariant();

        string response = normalizedCommandKey switch
        {
            "help" => BuildHelpMessage(availableCommands),
            "coords" => $"Coordinates: {actor.Position.X},{actor.Position.Y},{actor.Position.Z:0.##}",
            "roomid" => await DescribeRoomAsync(request.RoomId, cancellationToken),
            "roomstats" => await DescribeRoomStatsAsync(request.RoomId, cancellationToken),
            "chooser" => await DescribeRoomActorsAsync(request.RoomId, cancellationToken),
            "furni" => await DescribeRoomFurniAsync(request.RoomId, cancellationToken),
            "userinfo" => $"Actor {actor.DisplayName} at {actor.Position.X},{actor.Position.Y},{actor.Position.Z:0.##} walking={actor.IsWalking} typing={actor.IsTyping}.",
            "sign" => await SetSignAsync(request.RoomId, actor, parts.Skip(1).FirstOrDefault(), cancellationToken),
            "carry" => await SetCarryItemAsync(request.RoomId, actor, parts.Skip(1).FirstOrDefault(), cancellationToken),
            "lang" => await SetLanguageAsync(request.CharacterId, parts.Skip(1).FirstOrDefault(), cancellationToken),
            "wave" => await SetActorGestureAsync(request.RoomId, actor, "wav", "1", false, false, cancellationToken, "Wave status triggered."),
            "sit" => await SetActorGestureAsync(request.RoomId, actor, "sit", actor.Position.Z.ToString("0.##", CultureInfo.InvariantCulture), true, false, cancellationToken, "Actor posture set to sit."),
            "lay" => await SetActorGestureAsync(request.RoomId, actor, "lay", actor.Position.Z.ToString("0.##", CultureInfo.InvariantCulture), false, true, cancellationToken, "Actor posture set to lay."),
            "stand" => await ClearActorGestureAsync(request.RoomId, actor, cancellationToken),
            "whisper" => await SendDirectedChatAsync(request.RoomId, actor, parts.Skip(1).ToArray(), RoomChatMessageKind.Whisper, cancellationToken),
            "shout" => await SendDirectedChatAsync(request.RoomId, actor, parts.Skip(1).ToArray(), RoomChatMessageKind.Shout, cancellationToken),
            "roommute" => await ToggleRoomMuteAsync(request.RoomId, cancellationToken),
            "roomalert" => await SendRoomAlertAsync(request.RoomId, parts.Skip(1).ToArray(), cancellationToken),
            "pickall" => await PickAllAsync(request.CharacterId, request.RoomId, cancellationToken),
            "ha" => await BroadcastHotelAlertAsync(request.CharacterId, parts.Skip(1).ToArray(), cancellationToken),
            "alert" => await SendActorAlertAsync(request.CharacterId, request.RoomId, parts.Skip(1).ToArray(), cancellationToken),
            "kick" => await KickActorAsync(request.CharacterId, request.RoomId, parts.Skip(1).ToArray(), false, cancellationToken),
            "softkick" => await KickActorAsync(request.CharacterId, request.RoomId, parts.Skip(1).ToArray(), true, cancellationToken),
            "shutup" => await MuteActorAsync(request.CharacterId, request.RoomId, parts.Skip(1).ToArray(), cancellationToken),
            "unmute" => await UnmuteActorAsync(request.RoomId, parts.Skip(1).ToArray(), cancellationToken),
            "ban" => await BanActorAsync(request.CharacterId, request.RoomId, parts.Skip(1).ToArray(), false, cancellationToken),
            "superban" => await BanActorAsync(request.CharacterId, request.RoomId, parts.Skip(1).ToArray(), true, cancellationToken),
            "transfer" => await TransferCreditsAsync(request.CharacterId, request.RoomId, parts.Skip(1).ToArray(), cancellationToken),
            "rareweek" => await ManageRareOfTheWeekAsync(request.CharacterId, parts.Skip(1).ToArray(), cancellationToken),
            "gamesessions" => await DescribeGameSessionsAsync(parts.Skip(1).ToArray(), cancellationToken),
            "bbprepare" => await PrepareBattleBallAsync(parts.Skip(1).ToArray(), cancellationToken),
            "bbstart" => await StartBattleBallAsync(parts.Skip(1).ToArray(), cancellationToken),
            "bbscore" => await ScoreBattleBallAsync(parts.Skip(1).ToArray(), cancellationToken),
            "bbfinish" => await FinishBattleBallAsync(parts.Skip(1).ToArray(), cancellationToken),
            "ssprepare" => await PrepareSnowStormAsync(parts.Skip(1).ToArray(), cancellationToken),
            "ssstart" => await StartSnowStormAsync(parts.Skip(1).ToArray(), cancellationToken),
            "ssscore" => await ScoreSnowStormAsync(parts.Skip(1).ToArray(), cancellationToken),
            "ssfinish" => await FinishSnowStormAsync(parts.Skip(1).ToArray(), cancellationToken),
            "wsprepare" => await PrepareWobbleSquabbleAsync(parts.Skip(1).ToArray(), cancellationToken),
            "wsstart" => await StartWobbleSquabbleAsync(parts.Skip(1).ToArray(), cancellationToken),
            "wsscore" => await ScoreWobbleSquabbleAsync(parts.Skip(1).ToArray(), cancellationToken),
            "wsfinish" => await FinishWobbleSquabbleAsync(parts.Skip(1).ToArray(), cancellationToken),
            "kickall" => await KickAllAsync(request.CharacterId, request.RoomId, parts.Skip(1).ToArray(), cancellationToken),
            "lockdown" => ToggleLockdown(request.CharacterId, parts.Skip(1).ToArray()),
            "maintenance" => await ActivateMaintenanceAsync(request.CharacterId, parts.Skip(1).ToArray(), cancellationToken),
            _ => $"Command '{normalizedCommandKey}' is registered but not implemented in the runtime."
        };

        RoomChatMessage responseMessage = await _roomRuntimeRepository.AppendChatMessageAsync(
            request.RoomId,
            actor.ActorId,
            actor.DisplayName,
            response,
            RoomChatMessageKind.CommandResponse,
            cancellationToken);
        await _roomRuntimeCoordinator.SignalMutationAsync(
            request.RoomId,
            RoomRuntimeMutationKind.ChatMessageAppended,
            cancellationToken);

        if (string.Equals(normalizedCommandKey, "pickall", StringComparison.Ordinal))
        {
            await _roomRuntimeCoordinator.SignalMutationAsync(
                request.RoomId,
                RoomRuntimeMutationKind.RoomContentChanged,
                cancellationToken);
        }

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
        await _roomRuntimeCoordinator.SignalMutationAsync(
            roomId,
            RoomRuntimeMutationKind.ChatPolicyChanged,
            cancellationToken);
        return updatedPolicy.IsMuted ? "Room chat has been muted." : "Room chat has been unmuted.";
    }

    private async ValueTask<string> DescribeRoomAsync(
        RoomId roomId,
        CancellationToken cancellationToken)
    {
        RoomHotelSnapshot? room = await _hotelReadService.GetRoomSnapshotAsync(roomId, cancellationToken);
        if (room is null)
        {
            return "Room snapshot could not be resolved.";
        }

        return $"Room {room.Room.RoomId.Value}: '{room.Room.Name}' kind={room.Room.RoomKind} layout={room.Room.LayoutCode}.";
    }

    private async ValueTask<string> DescribeRoomStatsAsync(
        RoomId roomId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<RoomActorState> actors = await _roomRuntimeRepository.GetActorsByRoomIdAsync(roomId, cancellationToken);
        IReadOnlyList<RoomChatMessage> messages = await _roomRuntimeRepository.GetChatMessagesByRoomIdAsync(roomId, cancellationToken);
        RoomHotelSnapshot? room = await _hotelReadService.GetRoomSnapshotAsync(roomId, cancellationToken);

        int itemCount = room?.Items.Count ?? 0;
        return $"Room stats: actors={actors.Count}, items={itemCount}, chatMessages={messages.Count}.";
    }

    private async ValueTask<string> DescribeRoomActorsAsync(
        RoomId roomId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<RoomActorState> actors = await _roomRuntimeRepository.GetActorsByRoomIdAsync(roomId, cancellationToken);
        string[] playerNames = actors
            .Where(actor => actor.ActorKind == RoomActorKind.Player)
            .Select(actor => actor.DisplayName)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return playerNames.Length == 0
            ? "Chooser: no players are currently present."
            : $"Chooser: {string.Join(", ", playerNames)}";
    }

    private async ValueTask<string> DescribeRoomFurniAsync(
        RoomId roomId,
        CancellationToken cancellationToken)
    {
        RoomHotelSnapshot? room = await _hotelReadService.GetRoomSnapshotAsync(roomId, cancellationToken);
        if (room is null || room.Items.Count == 0)
        {
            return "Furni: no room items are currently present.";
        }

        string[] itemLabels = room.Items
            .Select(item => item.Definition?.PublicName ?? $"item:{item.Item.ItemDefinitionId.Value}")
            .OrderBy(label => label, StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .ToArray();

        return $"Furni: {string.Join(", ", itemLabels)}";
    }

    private async ValueTask<string> SetLanguageAsync(
        CharacterId characterId,
        string? languageCode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            InterfacePreferenceSnapshot snapshot = await _interfacePreferenceService.GetSnapshotAsync(characterId, cancellationToken);
            return $"Interface language is '{snapshot.SelectedLanguageCode}'.";
        }

        InterfacePreferenceSnapshot updatedSnapshot =
            await _interfacePreferenceService.SetLanguageAsync(characterId, languageCode, cancellationToken);
        return $"Interface language changed to '{updatedSnapshot.SelectedLanguageCode}'.";
    }

    private async ValueTask<string> SetActorGestureAsync(
        RoomId roomId,
        RoomActorState actor,
        string statusKey,
        string statusValue,
        bool isSitting,
        bool isLaying,
        CancellationToken cancellationToken,
        string successDetail)
    {
        IReadOnlyList<ActorStatusEntry> statusEntries = actor.StatusEntries
            .Where(entry =>
                !string.Equals(entry.Key, "wav", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(entry.Key, "sit", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(entry.Key, "lay", StringComparison.OrdinalIgnoreCase))
            .Concat([new ActorStatusEntry(statusKey, statusValue)])
            .ToArray();

        RoomActorState updatedActor = actor with
        {
            IsSitting = isSitting,
            IsLaying = isLaying,
            StatusEntries = statusEntries
        };

        await _roomRuntimeRepository.StoreActorStateAsync(roomId, updatedActor, cancellationToken);
        await _roomRuntimeCoordinator.SignalMutationAsync(
            roomId,
            RoomRuntimeMutationKind.ActorStateChanged,
            cancellationToken);

        return successDetail;
    }

    private async ValueTask<string> ClearActorGestureAsync(
        RoomId roomId,
        RoomActorState actor,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ActorStatusEntry> statusEntries = actor.StatusEntries
            .Where(entry =>
                !string.Equals(entry.Key, "wav", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(entry.Key, "sit", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(entry.Key, "lay", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        RoomActorState updatedActor = actor with
        {
            IsSitting = false,
            IsLaying = false,
            StatusEntries = statusEntries
        };

        await _roomRuntimeRepository.StoreActorStateAsync(roomId, updatedActor, cancellationToken);
        await _roomRuntimeCoordinator.SignalMutationAsync(
            roomId,
            RoomRuntimeMutationKind.ActorStateChanged,
            cancellationToken);

        return "Actor posture reset to stand.";
    }

    private async ValueTask<string> SendRoomAlertAsync(
        RoomId roomId,
        string[] arguments,
        CancellationToken cancellationToken)
    {
        string alertText = string.Join(' ', arguments).Trim();
        if (string.IsNullOrWhiteSpace(alertText))
        {
            return "Usage: :roomalert <message>";
        }

        await _roomRuntimeRepository.AppendChatMessageAsync(
            roomId,
            0,
            "Room Alert",
            alertText,
            RoomChatMessageKind.System,
            cancellationToken);
        await _roomRuntimeCoordinator.SignalMutationAsync(
            roomId,
            RoomRuntimeMutationKind.ChatMessageAppended,
            cancellationToken);

        return $"Room alert sent: {alertText}";
    }

    private async ValueTask<string> SendDirectedChatAsync(
        RoomId roomId,
        RoomActorState actor,
        string[] arguments,
        RoomChatMessageKind messageKind,
        CancellationToken cancellationToken)
    {
        if (arguments.Length < 2)
        {
            return messageKind == RoomChatMessageKind.Whisper
                ? "Usage: :whisper <username> <message>"
                : "Usage: :shout <username> <message>";
        }

        RoomActorState? targetActor = await ResolvePlayerActorByNameAsync(roomId, arguments[0], cancellationToken);
        if (targetActor is null)
        {
            return $"User '{arguments[0]}' is not present in this room.";
        }

        string message = NormalizeChatText(string.Join(' ', arguments.Skip(1)));
        if (string.IsNullOrWhiteSpace(message))
        {
            return "Message cannot be empty.";
        }

        string renderedMessage = messageKind == RoomChatMessageKind.Whisper
            ? $"[Whisper to {targetActor.DisplayName}] {message}"
            : $"[Shout to {targetActor.DisplayName}] {message.ToUpperInvariant()}";

        await _roomRuntimeRepository.AppendChatMessageAsync(
            roomId,
            actor.ActorId,
            actor.DisplayName,
            renderedMessage,
            messageKind,
            cancellationToken);
        await _roomRuntimeCoordinator.SignalMutationAsync(
            roomId,
            RoomRuntimeMutationKind.ChatMessageAppended,
            cancellationToken);

        return messageKind == RoomChatMessageKind.Whisper
            ? $"Whisper sent to {targetActor.DisplayName}."
            : $"Shout sent to {targetActor.DisplayName}.";
    }

    private async ValueTask<string> PickAllAsync(
        CharacterId characterId,
        RoomId roomId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<RoomItemState> removedItems = await _roomItemRepository.RemoveByRoomIdAsync(roomId, cancellationToken);
        if (removedItems.Count == 0)
        {
            return "No room items were available to return to storage.";
        }

        InventoryItemState[] inventoryItems = removedItems
            .Select(item => new InventoryItemState(
                item.ItemId,
                characterId,
                item.ItemDefinitionId,
                item.StateData,
                DateTime.UtcNow))
            .ToArray();

        await _inventoryRepository.AddExistingItemsAsync(characterId, inventoryItems, cancellationToken);
        await _roomRuntimeCoordinator.SignalMutationAsync(
            roomId,
            RoomRuntimeMutationKind.RoomContentChanged,
            cancellationToken);

        return $"{removedItems.Count} room item(s) were returned to storage.";
    }

    private async ValueTask<string> BroadcastHotelAlertAsync(
        CharacterId senderCharacterId,
        string[] arguments,
        CancellationToken cancellationToken)
    {
        string alertText = string.Join(' ', arguments).Trim();
        if (string.IsNullOrWhiteSpace(alertText))
        {
            return "Usage: :ha <message>";
        }

        CharacterProfile? sender = await _characterProfileRepository.GetByIdAsync(senderCharacterId, cancellationToken);
        string senderName = sender?.Username ?? "Hotel";

        IReadOnlyList<RoomId> activeRooms = await _roomRuntimeRepository.GetAllActiveRoomIdsAsync(cancellationToken);
        foreach (RoomId targetRoomId in activeRooms)
        {
            await _roomRuntimeRepository.AppendChatMessageAsync(
                targetRoomId,
                senderCharacterId.Value,
                senderName,
                $"[Hotel Alert] {alertText}",
                RoomChatMessageKind.System,
                cancellationToken);
            await _roomRuntimeCoordinator.SignalMutationAsync(
                targetRoomId,
                RoomRuntimeMutationKind.ChatMessageAppended,
                cancellationToken);
        }

        return activeRooms.Count == 0
            ? $"Hotel alert sent: {alertText} (no active rooms)"
            : $"Hotel alert sent to {activeRooms.Count} room(s): {alertText}";
    }

    private async ValueTask<string> KickAllAsync(
        CharacterId moderatorCharacterId,
        RoomId currentRoomId,
        string[] arguments,
        CancellationToken cancellationToken)
    {
        // :kickall          — evict all players from every active room
        // :kickall <roomId> — evict all players from the specified room only
        if (arguments.Length > 0 &&
            long.TryParse(arguments[0], NumberStyles.None, CultureInfo.InvariantCulture, out long targetRoomIdValue) &&
            targetRoomIdValue > 0)
        {
            RoomId targetRoomId = new(targetRoomIdValue);
            int evicted = await _roomRuntimeRepository.EvictAllPlayersFromRoomAsync(targetRoomId, cancellationToken);
            await _roomRuntimeCoordinator.SignalMutationAsync(
                targetRoomId,
                RoomRuntimeMutationKind.ActorPresenceChanged,
                cancellationToken);
            return $"Evicted {evicted} player(s) from room {targetRoomIdValue}.";
        }

        IReadOnlyList<RoomId> activeRooms = await _roomRuntimeRepository.GetAllActiveRoomIdsAsync(cancellationToken);
        int totalEvicted = 0;
        foreach (RoomId roomId in activeRooms)
        {
            totalEvicted += await _roomRuntimeRepository.EvictAllPlayersFromRoomAsync(roomId, cancellationToken);
            await _roomRuntimeCoordinator.SignalMutationAsync(
                roomId,
                RoomRuntimeMutationKind.ActorPresenceChanged,
                cancellationToken);
        }

        return $"Evicted {totalEvicted} player(s) from {activeRooms.Count} room(s).";
    }

    private string ToggleLockdown(CharacterId moderatorCharacterId, string[] arguments)
    {
        // :lockdown           — toggle
        // :lockdown on        — activate
        // :lockdown on <msg>  — activate with custom message
        // :lockdown off       — deactivate
        if (arguments.Length == 0)
        {
            if (_hotelOperationalState.IsLockdownActive)
            {
                _hotelOperationalState.DeactivateLockdown();
                return "Hotel lockdown deactivated.";
            }

            _hotelOperationalState.ActivateLockdown(null);
            return "Hotel lockdown activated. New room entries are blocked.";
        }

        if (IsSameToken(arguments[0], "off"))
        {
            _hotelOperationalState.DeactivateLockdown();
            return "Hotel lockdown deactivated. Room entry is restored.";
        }

        string message = arguments.Length > 1
            ? string.Join(' ', arguments.Skip(1)).Trim()
            : null!;

        _hotelOperationalState.ActivateLockdown(string.IsNullOrWhiteSpace(message) ? null : message);
        return string.IsNullOrWhiteSpace(message)
            ? "Hotel lockdown activated. New room entries are blocked."
            : $"Hotel lockdown activated with message: {message}";
    }

    private async ValueTask<string> ActivateMaintenanceAsync(
        CharacterId moderatorCharacterId,
        string[] arguments,
        CancellationToken cancellationToken)
    {
        string message = arguments.Length > 0
            ? string.Join(' ', arguments).Trim()
            : "The hotel is temporarily under maintenance. Please try again soon.";

        // Activate lockdown first so no new entries occur during the eviction.
        _hotelOperationalState.ActivateLockdown(message);

        // Broadcast the maintenance alert to every active room.
        CharacterProfile? sender = await _characterProfileRepository.GetByIdAsync(moderatorCharacterId, cancellationToken);
        string senderName = sender?.Username ?? "Hotel";

        IReadOnlyList<RoomId> activeRooms = await _roomRuntimeRepository.GetAllActiveRoomIdsAsync(cancellationToken);
        foreach (RoomId roomId in activeRooms)
        {
            await _roomRuntimeRepository.AppendChatMessageAsync(
                roomId,
                moderatorCharacterId.Value,
                senderName,
                $"[Maintenance] {message}",
                RoomChatMessageKind.System,
                cancellationToken);
            await _roomRuntimeCoordinator.SignalMutationAsync(
                roomId,
                RoomRuntimeMutationKind.ChatMessageAppended,
                cancellationToken);
        }

        return $"Maintenance mode activated. {activeRooms.Count} room(s) notified. New entries are blocked.";
    }

    private void PruneExpiredEntries()
    {
        DateTime utcNow = DateTime.UtcNow;

        // Remove expired mute entries.
        foreach (string key in _actorMutes.Keys.ToArray())
        {
            if (_actorMutes.TryGetValue(key, out DateTime mutedUntil) && mutedUntil <= utcNow)
            {
                _actorMutes.TryRemove(key, out _);
            }
        }

        // Remove flood-window entries where the window queue is now empty.
        foreach (string key in _chatWindows.Keys.ToArray())
        {
            object? syncRoot = _chatWindowLocks.GetOrAdd(key, static _ => new object());
            lock (syncRoot)
            {
                if (!_chatWindows.TryGetValue(key, out Queue<DateTime>? window))
                {
                    continue;
                }

                // Drain timestamps that have already aged out.
                while (window.Count > 0 && window.Peek() < utcNow)
                {
                    window.Dequeue();
                }

                if (window.Count == 0)
                {
                    _chatWindows.TryRemove(key, out _);
                    _chatWindowLocks.TryRemove(key, out _);
                }
            }
        }
    }

    private async ValueTask<string> SendActorAlertAsync(
        CharacterId moderatorCharacterId,
        RoomId roomId,
        string[] arguments,
        CancellationToken cancellationToken)
    {
        if (!TrySplitTargetAndMessage(arguments, out string targetName, out string message))
        {
            return "Usage: :alert <username> <message>";
        }

        RoomActorState? targetActor = await ResolvePlayerActorByNameAsync(roomId, targetName, cancellationToken);
        if (targetActor is null)
        {
            return $"User '{targetName}' is not present in this room.";
        }

        CharacterProfile? moderator = await _characterProfileRepository.GetByIdAsync(moderatorCharacterId, cancellationToken);
        string senderName = moderator?.Username ?? "Moderator";

        await _roomRuntimeRepository.AppendChatMessageAsync(
            roomId,
            moderatorCharacterId.Value,
            senderName,
            $"[Alert to {targetActor.DisplayName}] {message}",
            RoomChatMessageKind.System,
            cancellationToken);
        await _roomRuntimeCoordinator.SignalMutationAsync(
            roomId,
            RoomRuntimeMutationKind.ChatMessageAppended,
            cancellationToken);

        return $"Alert sent to {targetActor.DisplayName}.";
    }

    private async ValueTask<string> KickActorAsync(
        CharacterId moderatorCharacterId,
        RoomId roomId,
        string[] arguments,
        bool isSoftKick,
        CancellationToken cancellationToken)
    {
        if (arguments.Length == 0)
        {
            return isSoftKick
                ? "Usage: :softkick <username>"
                : "Usage: :kick <username>";
        }

        RoomActorState? targetActor = await ResolvePlayerActorByNameAsync(roomId, arguments[0], cancellationToken);
        if (targetActor is null)
        {
            return $"User '{arguments[0]}' is not present in this room.";
        }

        if (targetActor.ActorId == moderatorCharacterId.Value)
        {
            return "You cannot kick yourself.";
        }

        await _roomRuntimeRepository.RemoveActorFromAllRoomsAsync(targetActor.ActorId, cancellationToken);
        await _roomRuntimeCoordinator.SignalMutationAsync(
            roomId,
            RoomRuntimeMutationKind.ActorPresenceChanged,
            cancellationToken);

        return isSoftKick
            ? $"{targetActor.DisplayName} was soft-kicked from the room."
            : $"{targetActor.DisplayName} was kicked from the room.";
    }

    private async ValueTask<string> MuteActorAsync(
        CharacterId moderatorCharacterId,
        RoomId roomId,
        string[] arguments,
        CancellationToken cancellationToken)
    {
        if (arguments.Length == 0)
        {
            return "Usage: :shutup <username>";
        }

        RoomActorState? targetActor = await ResolvePlayerActorByNameAsync(roomId, arguments[0], cancellationToken);
        if (targetActor is null)
        {
            return $"User '{arguments[0]}' is not present in this room.";
        }

        if (targetActor.ActorId == moderatorCharacterId.Value)
        {
            return "You cannot mute yourself.";
        }

        DateTime mutedUntilUtc = DateTime.UtcNow.AddMinutes(2);
        _actorMutes[BuildActorMuteKey(roomId, new CharacterId(targetActor.ActorId))] = mutedUntilUtc;
        return $"{targetActor.DisplayName} was muted until {mutedUntilUtc:O}.";
    }

    private ValueTask<string> UnmuteActorAsync(
        RoomId roomId,
        string[] arguments,
        CancellationToken cancellationToken)
    {
        if (arguments.Length == 0)
        {
            return ValueTask.FromResult("Usage: :unmute <username>");
        }

        return UnmuteActorCoreAsync(roomId, arguments[0], cancellationToken);
    }

    private async ValueTask<string> BanActorAsync(
        CharacterId moderatorCharacterId,
        RoomId roomId,
        string[] arguments,
        bool permanent,
        CancellationToken cancellationToken)
    {
        int durationSeconds = 0;
        if (permanent)
        {
            if (arguments.Length < 2)
            {
                return "Usage: :superban <username> <reason>";
            }
        }
        else if (arguments.Length < 3 ||
                 !int.TryParse(arguments[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out durationSeconds) ||
                 durationSeconds <= 0)
        {
            return "Usage: :ban <username> <seconds> <reason>";
        }

        string targetName = arguments[0];
        RoomActorState? targetActor = await ResolvePlayerActorByNameAsync(roomId, targetName, cancellationToken);
        if (targetActor is null)
        {
            return $"User '{targetName}' is not present in this room.";
        }

        if (targetActor.ActorId == moderatorCharacterId.Value)
        {
            return "You cannot ban yourself.";
        }
        string reason = permanent
            ? string.Join(' ', arguments.Skip(1)).Trim()
            : string.Join(' ', arguments.Skip(2)).Trim();

        if (string.IsNullOrWhiteSpace(reason))
        {
            return permanent
                ? "Usage: :superban <username> <reason>"
                : "Usage: :ban <username> <seconds> <reason>";
        }

        DateTime? expiresAtUtc = permanent ? null : DateTime.UtcNow.AddSeconds(durationSeconds);
        await _moderationRepository.StoreBanAsync(
            new ModerationBanRecord(
                new CharacterId(targetActor.ActorId),
                moderatorCharacterId,
                DateTime.UtcNow,
                expiresAtUtc,
                reason,
                permanent),
            cancellationToken);

        await _roomRuntimeRepository.RemoveActorFromAllRoomsAsync(targetActor.ActorId, cancellationToken);
        await _roomRuntimeCoordinator.SignalMutationAsync(
            roomId,
            RoomRuntimeMutationKind.ActorPresenceChanged,
            cancellationToken);

        return permanent
            ? $"{targetActor.DisplayName} was permanently banned."
            : $"{targetActor.DisplayName} was banned for {durationSeconds} second(s).";
    }

    private async ValueTask<string> TransferCreditsAsync(
        CharacterId moderatorCharacterId,
        RoomId roomId,
        string[] arguments,
        CancellationToken cancellationToken)
    {
        if (arguments.Length < 2 ||
            !int.TryParse(arguments[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int credits) ||
            credits <= 0 ||
            credits > 1_000_000)
        {
            return "Usage: :transfer <username> <credits> (max 1,000,000 per transfer)";
        }

        RoomActorState? targetActor = await ResolvePlayerActorByNameAsync(roomId, arguments[0], cancellationToken);
        if (targetActor is null)
        {
            return $"User '{arguments[0]}' is not present in this room.";
        }

        WalletSnapshot wallet =
            await _walletRepository.GetByCharacterIdAsync(new CharacterId(targetActor.ActorId), cancellationToken)
            ?? new WalletSnapshot(new CharacterId(targetActor.ActorId), [], []);

        List<WalletBalance> balances = wallet.Balances.ToList();
        WalletBalance? existingCredits = balances.FirstOrDefault(balance =>
            string.Equals(balance.CurrencyCode, "credits", StringComparison.OrdinalIgnoreCase));
        if (existingCredits is null)
        {
            balances.Add(new WalletBalance("credits", credits));
        }
        else
        {
            int index = balances.IndexOf(existingCredits);
            balances[index] = existingCredits with { Amount = checked(existingCredits.Amount + credits) };
        }

        List<WalletLedgerEntry> ledger = wallet.RecentEntries.ToList();
        ledger.Insert(
            0,
            new WalletLedgerEntry(
                "credits",
                credits,
                $"moderation_transfer:from:{moderatorCharacterId.Value}",
                DateTime.UtcNow));

        await _walletRepository.StoreAsync(
            new WalletSnapshot(wallet.CharacterId, balances, ledger.Take(10).ToArray()),
            cancellationToken);

        return $"{credits} credit(s) were transferred to {targetActor.DisplayName}.";
    }

    private async ValueTask<string> DescribeGameSessionsAsync(
        string[] arguments,
        CancellationToken cancellationToken)
    {
        if (arguments.Length == 0)
        {
            IReadOnlyList<GameSessionState> sessions = await _gameRuntimeService.GetActiveSessionsAsync(cancellationToken);
            if (sessions.Count == 0)
            {
                return "No active game sessions are currently registered.";
            }

            string[] labels = sessions
                .Select(session => $"{session.SessionKey}({session.GameKey}:{session.Status})")
                .ToArray();
            return $"Active game sessions: {string.Join(", ", labels)}";
        }

        GameSessionState? sessionState = await _gameRuntimeService.GetSessionAsync(arguments[0], cancellationToken);
        if (sessionState is null)
        {
            return $"Game session '{arguments[0]}' could not be resolved.";
        }

        return $"Game session {sessionState.SessionKey}: game={sessionState.GameKey} status={sessionState.Status} phase={sessionState.PhaseCode} players={sessionState.Players.Count}/{sessionState.MaximumPlayers}.";
    }

    private async ValueTask<string> PrepareBattleBallAsync(
        string[] arguments,
        CancellationToken cancellationToken)
    {
        if (arguments.Length == 0)
        {
            return "Usage: :bbprepare <sessionKey>";
        }

        GameSessionUpdateResult result = await _battleBallLifecycleService.PrepareMatchAsync(arguments[0], cancellationToken);
        return result.Detail;
    }

    private async ValueTask<string> StartBattleBallAsync(
        string[] arguments,
        CancellationToken cancellationToken)
    {
        if (arguments.Length == 0)
        {
            return "Usage: :bbstart <sessionKey>";
        }

        GameSessionUpdateResult result = await _battleBallLifecycleService.StartRoundAsync(arguments[0], cancellationToken);
        return result.Detail;
    }

    private async ValueTask<string> ScoreBattleBallAsync(
        string[] arguments,
        CancellationToken cancellationToken)
    {
        if (arguments.Length < 3 ||
            !int.TryParse(arguments[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int points))
        {
            return "Usage: :bbscore <sessionKey> <teamKey> <points>";
        }

        GameSessionUpdateResult result =
            await _battleBallLifecycleService.AwardPointsAsync(arguments[0], arguments[1], points, cancellationToken);
        return result.Detail;
    }

    private async ValueTask<string> FinishBattleBallAsync(
        string[] arguments,
        CancellationToken cancellationToken)
    {
        if (arguments.Length == 0)
        {
            return "Usage: :bbfinish <sessionKey>";
        }

        GameSessionUpdateResult result = await _battleBallLifecycleService.FinishMatchAsync(arguments[0], cancellationToken);
        return result.Detail;
    }

    private async ValueTask<string> PrepareSnowStormAsync(
        string[] arguments,
        CancellationToken cancellationToken)
    {
        if (arguments.Length == 0)
        {
            return "Usage: :ssprepare <sessionKey>";
        }

        GameSessionUpdateResult result = await _snowStormLifecycleService.PrepareMatchAsync(arguments[0], cancellationToken);
        return result.Detail;
    }

    private async ValueTask<string> StartSnowStormAsync(
        string[] arguments,
        CancellationToken cancellationToken)
    {
        if (arguments.Length == 0)
        {
            return "Usage: :ssstart <sessionKey>";
        }

        GameSessionUpdateResult result = await _snowStormLifecycleService.StartBattleAsync(arguments[0], cancellationToken);
        return result.Detail;
    }

    private async ValueTask<string> ScoreSnowStormAsync(
        string[] arguments,
        CancellationToken cancellationToken)
    {
        if (arguments.Length < 3 ||
            !int.TryParse(arguments[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int points))
        {
            return "Usage: :ssscore <sessionKey> <teamKey> <points>";
        }

        GameSessionUpdateResult result =
            await _snowStormLifecycleService.AwardPointsAsync(arguments[0], arguments[1], points, cancellationToken);
        return result.Detail;
    }

    private async ValueTask<string> FinishSnowStormAsync(
        string[] arguments,
        CancellationToken cancellationToken)
    {
        if (arguments.Length == 0)
        {
            return "Usage: :ssfinish <sessionKey>";
        }

        GameSessionUpdateResult result = await _snowStormLifecycleService.FinishMatchAsync(arguments[0], cancellationToken);
        return result.Detail;
    }

    private async ValueTask<string> PrepareWobbleSquabbleAsync(
        string[] arguments,
        CancellationToken cancellationToken)
    {
        if (arguments.Length == 0)
        {
            return "Usage: :wsprepare <sessionKey>";
        }

        GameSessionUpdateResult result = await _wobbleSquabbleLifecycleService.PrepareDuelAsync(arguments[0], cancellationToken);
        return result.Detail;
    }

    private async ValueTask<string> StartWobbleSquabbleAsync(
        string[] arguments,
        CancellationToken cancellationToken)
    {
        if (arguments.Length == 0)
        {
            return "Usage: :wsstart <sessionKey>";
        }

        GameSessionUpdateResult result = await _wobbleSquabbleLifecycleService.StartDuelAsync(arguments[0], cancellationToken);
        return result.Detail;
    }

    private async ValueTask<string> ScoreWobbleSquabbleAsync(
        string[] arguments,
        CancellationToken cancellationToken)
    {
        if (arguments.Length < 3 ||
            !int.TryParse(arguments[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int points))
        {
            return "Usage: :wsscore <sessionKey> <teamKey> <points>";
        }

        GameSessionUpdateResult result =
            await _wobbleSquabbleLifecycleService.AwardPointsAsync(arguments[0], arguments[1], points, cancellationToken);
        return result.Detail;
    }

    private async ValueTask<string> FinishWobbleSquabbleAsync(
        string[] arguments,
        CancellationToken cancellationToken)
    {
        if (arguments.Length == 0)
        {
            return "Usage: :wsfinish <sessionKey>";
        }

        GameSessionUpdateResult result = await _wobbleSquabbleLifecycleService.FinishDuelAsync(arguments[0], cancellationToken);
        return result.Detail;
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
        await _roomRuntimeCoordinator.SignalMutationAsync(
            roomId,
            RoomRuntimeMutationKind.ActorStateChanged,
            cancellationToken);
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
        await _roomRuntimeCoordinator.SignalMutationAsync(
            roomId,
            RoomRuntimeMutationKind.ActorStateChanged,
            cancellationToken);
        return carryItem is null
            ? "Carry item cleared."
            : $"Carry item set to {carryItem.ItemTypeId}.";
    }

    private async ValueTask<string> ManageRareOfTheWeekAsync(
        CharacterId characterId,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        CatalogFeatureState currentState =
            await _catalogFeatureStateRepository.GetByFeatureKeyAsync(CatalogFeatureKeys.RareOfTheWeek, cancellationToken)
            ?? new CatalogFeatureState(
                FeatureKey: CatalogFeatureKeys.RareOfTheWeek,
                IsEnabled: false,
                FeaturedOfferId: null,
                UpdatedAt: DateTimeOffset.UtcNow,
                UpdatedBy: $"character:{characterId.Value}");

        if (arguments.Count == 0 || IsSameToken(arguments[0], "show"))
        {
            return DescribeRareOfTheWeek(currentState);
        }

        if (IsSameToken(arguments[0], "on"))
        {
            if (currentState.FeaturedOfferId is null)
            {
                return "Rare Of The Week cannot be enabled until an offer is selected.";
            }

            CatalogFeatureState enabledState = currentState with
            {
                IsEnabled = true,
                UpdatedAt = DateTimeOffset.UtcNow,
                UpdatedBy = $"character:{characterId.Value}"
            };

            await _catalogFeatureStateRepository.StoreAsync(enabledState, cancellationToken);
            return DescribeRareOfTheWeek(enabledState);
        }

        if (IsSameToken(arguments[0], "off"))
        {
            CatalogFeatureState disabledState = currentState with
            {
                IsEnabled = false,
                UpdatedAt = DateTimeOffset.UtcNow,
                UpdatedBy = $"character:{characterId.Value}"
            };

            await _catalogFeatureStateRepository.StoreAsync(disabledState, cancellationToken);
            return DescribeRareOfTheWeek(disabledState);
        }

        if (IsSameToken(arguments[0], "set"))
        {
            if (arguments.Count < 2 ||
                !long.TryParse(arguments[1], NumberStyles.None, CultureInfo.InvariantCulture, out long offerIdValue) ||
                offerIdValue <= 0)
            {
                return "Usage: :rareweek set <offerId>";
            }

            CatalogOfferDefinition? offer = await _catalogOfferRepository.GetByIdAsync(
                new CatalogOfferId(offerIdValue),
                cancellationToken);
            if (offer is null)
            {
                return $"Catalog offer {offerIdValue} could not be found.";
            }

            CatalogFeatureState updatedState = currentState with
            {
                FeaturedOfferId = offer.CatalogOfferId,
                IsEnabled = true,
                UpdatedAt = DateTimeOffset.UtcNow,
                UpdatedBy = $"character:{characterId.Value}"
            };

            await _catalogFeatureStateRepository.StoreAsync(updatedState, cancellationToken);
            return $"Rare Of The Week now points to offer {offer.CatalogOfferId.Value} ({offer.CatalogName}) and is enabled.";
        }

        return "Usage: :rareweek [show|on|off|set <offerId>]";
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

    private static string DescribeRareOfTheWeek(CatalogFeatureState state)
    {
        string offerLabel = state.FeaturedOfferId is null
            ? "none"
            : state.FeaturedOfferId.Value.Value.ToString(CultureInfo.InvariantCulture);
        string enabledLabel = state.IsEnabled ? "enabled" : "disabled";
        return $"Rare Of The Week is {enabledLabel}. Offer={offerLabel}. UpdatedBy={state.UpdatedBy}.";
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

    private static bool IsSameToken(string candidate, string expected)
    {
        return string.Equals(candidate, expected, StringComparison.OrdinalIgnoreCase);
    }

    public static string NormalizeChatText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        StringBuilder builder = new(value.Length);
        bool previousWasWhitespace = false;

        foreach (char character in value)
        {
            if (char.IsControl(character))
            {
                if (character is '\t' or '\r' or '\n')
                {
                    if (!previousWasWhitespace)
                    {
                        builder.Append(' ');
                        previousWasWhitespace = true;
                    }
                }

                continue;
            }

            if (char.IsWhiteSpace(character))
            {
                if (!previousWasWhitespace)
                {
                    builder.Append(' ');
                    previousWasWhitespace = true;
                }

                continue;
            }

            builder.Append(character);
            previousWasWhitespace = false;
        }

        return builder.ToString().Trim();
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

    private async ValueTask<RoomActorState?> ResolvePlayerActorByNameAsync(
        RoomId roomId,
        string targetName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(targetName))
        {
            return null;
        }

        IReadOnlyList<RoomActorState> actors = await _roomRuntimeRepository.GetActorsByRoomIdAsync(roomId, cancellationToken);
        return actors.FirstOrDefault(actor =>
            actor.ActorKind == RoomActorKind.Player &&
            string.Equals(actor.DisplayName, targetName, StringComparison.OrdinalIgnoreCase));
    }

    private bool TryGetActiveActorMuteUntil(
        RoomId roomId,
        CharacterId characterId,
        out DateTime mutedUntilUtc)
    {
        string key = BuildActorMuteKey(roomId, characterId);
        if (!_actorMutes.TryGetValue(key, out mutedUntilUtc))
        {
            return false;
        }

        if (mutedUntilUtc <= DateTime.UtcNow)
        {
            _actorMutes.TryRemove(key, out _);
            return false;
        }

        return true;
    }

    private static bool TrySplitTargetAndMessage(
        string[] arguments,
        out string targetName,
        out string message)
    {
        targetName = string.Empty;
        message = string.Empty;

        if (arguments.Length < 2)
        {
            return false;
        }

        targetName = arguments[0];
        message = string.Join(' ', arguments.Skip(1)).Trim();
        return !string.IsNullOrWhiteSpace(targetName) && !string.IsNullOrWhiteSpace(message);
    }

    private static string BuildActorMuteKey(RoomId roomId, CharacterId characterId)
    {
        return $"{roomId.Value}:{characterId.Value}";
    }

    private async ValueTask<string> UnmuteActorCoreAsync(
        RoomId roomId,
        string targetName,
        CancellationToken cancellationToken)
    {
        RoomActorState? targetActor = await ResolvePlayerActorByNameAsync(roomId, targetName, cancellationToken);
        if (targetActor is null)
        {
            return $"User '{targetName}' is not present in this room.";
        }

        string actorMuteKey = BuildActorMuteKey(roomId, new CharacterId(targetActor.ActorId));
        bool removed = _actorMutes.TryRemove(actorMuteKey, out _);
        return removed
            ? $"{targetActor.DisplayName} was unmuted."
            : $"{targetActor.DisplayName} is not currently muted.";
    }
}
