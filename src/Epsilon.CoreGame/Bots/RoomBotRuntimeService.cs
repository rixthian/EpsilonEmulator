using Epsilon.Content;
using Epsilon.Rooms;

namespace Epsilon.CoreGame;

public sealed class RoomBotRuntimeService : IRoomBotRuntimeService
{
    private readonly IRoomBotDefinitionRepository _botDefinitionRepository;
    private readonly IHotelReadService _hotelReadService;
    private readonly INavigatorPublicRoomRepository _navigatorPublicRoomRepository;
    private readonly IRoomRuntimeRepository _roomRuntimeRepository;
    private readonly IRoomRuntimeCoordinator _roomRuntimeCoordinator;
    private readonly IHotelEventBus _hotelEventBus;

    public RoomBotRuntimeService(
        IRoomBotDefinitionRepository botDefinitionRepository,
        IHotelReadService hotelReadService,
        INavigatorPublicRoomRepository navigatorPublicRoomRepository,
        IRoomRuntimeRepository roomRuntimeRepository,
        IRoomRuntimeCoordinator roomRuntimeCoordinator,
        IHotelEventBus hotelEventBus)
    {
        _botDefinitionRepository = botDefinitionRepository;
        _hotelReadService = hotelReadService;
        _navigatorPublicRoomRepository = navigatorPublicRoomRepository;
        _roomRuntimeRepository = roomRuntimeRepository;
        _roomRuntimeCoordinator = roomRuntimeCoordinator;
        _hotelEventBus = hotelEventBus;
    }

    public async ValueTask<IReadOnlyList<RoomActorState>> EnsureRoomBotsAsync(
        RoomHotelSnapshot room,
        NavigatorPublicRoomDefinition? publicRoomEntry,
        CancellationToken cancellationToken = default)
    {
        if (room.Layout is null)
        {
            return [];
        }

        IReadOnlyList<HotelBotDefinition> botDefinitions =
            await GetRoomBotDefinitionsAsync(room.Room.RoomId, publicRoomEntry, cancellationToken);

        if (botDefinitions.Count == 0)
        {
            return [];
        }

        List<RoomActorState> materializedBots = [];
        bool anyCreated = false;

        // Resolve actor IDs for all bots upfront and detect hash collisions within
        // the batch. If two different bot keys produce the same ID, the later one
        // gets a deterministic offset applied until the slot is free.
        HashSet<long> assignedIds = [];
        IReadOnlyList<HotelBotDefinition> enabledBots = botDefinitions.Where(static bot => bot.IsEnabled).ToArray();
        Dictionary<string, long> botActorIds = new(StringComparer.OrdinalIgnoreCase);
        foreach (HotelBotDefinition bot in enabledBots)
        {
            long actorId = BuildBotActorId(bot.BotKey);
            int offset = 0;
            while (!assignedIds.Add(actorId))
            {
                // Collision within this batch — nudge the ID by a fixed step until free.
                offset++;
                actorId = BuildBotActorId(bot.BotKey) - offset;
            }

            botActorIds[bot.BotKey] = actorId;
        }

        foreach (HotelBotDefinition bot in enabledBots)
        {
            long actorId = botActorIds[bot.BotKey];
            RoomActorState? existingActor = await _roomRuntimeRepository.GetActorByIdAsync(
                room.Room.RoomId,
                actorId,
                cancellationToken);

            RoomActorState botActor = BuildBotActorState(room.Room.RoomId, room.Layout, bot, actorId);
            if (existingActor is not null && existingActor == botActor)
            {
                materializedBots.Add(existingActor);
                continue;
            }

            await _roomRuntimeRepository.StoreActorStateAsync(room.Room.RoomId, botActor, cancellationToken);
            materializedBots.Add(botActor);
            anyCreated = true;
        }

        if (anyCreated)
        {
            await _roomRuntimeCoordinator.SignalMutationAsync(
                room.Room.RoomId,
                RoomRuntimeMutationKind.ActorPresenceChanged,
                cancellationToken);
        }

        return materializedBots;
    }

    public async ValueTask<RoomChatMessage?> TryHandlePlayerChatAsync(
        RoomId roomId,
        CharacterId characterId,
        string message,
        CancellationToken cancellationToken = default)
    {
        NavigatorPublicRoomDefinition? publicRoomEntry =
            await _navigatorPublicRoomRepository.GetByRoomIdAsync(roomId, cancellationToken);
        RoomHotelSnapshot? room = await _hotelReadService.GetRoomSnapshotAsync(roomId, cancellationToken);
        if (room is null)
        {
            return null;
        }

        IReadOnlyList<HotelBotDefinition> botDefinitions =
            await GetRoomBotDefinitionsAsync(roomId, publicRoomEntry, cancellationToken);
        if (botDefinitions.Count == 0)
        {
            return null;
        }

        string normalizedMessage = message.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedMessage))
        {
            return null;
        }

        RoomActorState? playerActor = await _roomRuntimeRepository.GetActorByIdAsync(
            roomId,
            characterId.Value,
            cancellationToken);
        if (playerActor is null)
        {
            return null;
        }

        foreach (HotelBotDefinition botDefinition in botDefinitions.Where(static bot => bot.IsEnabled))
        {
            BotReplyDefinition? reply = botDefinition.Replies.FirstOrDefault(candidate =>
                candidate.TriggerKeywords.Any(keyword =>
                    normalizedMessage.Contains(keyword, StringComparison.OrdinalIgnoreCase)));

            if (reply is null)
            {
                continue;
            }

            long botActorId = BuildBotActorId(botDefinition.BotKey);
            RoomActorState? botActor = await _roomRuntimeRepository.GetActorByIdAsync(roomId, botActorId, cancellationToken);
            if (botActor is null)
            {
                continue;
            }

            if (reply.GrantedCarryItemTypeId is int carryItemTypeId &&
                !string.IsNullOrWhiteSpace(reply.GrantedCarryItemName))
            {
                RoomActorState updatedPlayer = playerActor with
                {
                    CarryItem = new CarryItemState(carryItemTypeId, reply.GrantedCarryItemName, DateTime.UtcNow.AddMinutes(5))
                };

                await _roomRuntimeRepository.StoreActorStateAsync(roomId, updatedPlayer, cancellationToken);
                await _roomRuntimeCoordinator.SignalMutationAsync(
                    roomId,
                    RoomRuntimeMutationKind.ActorStateChanged,
                    cancellationToken);
            }

            RoomChatMessage botMessage = await _roomRuntimeRepository.AppendChatMessageAsync(
                roomId,
                botActor.ActorId,
                botActor.DisplayName,
                reply.ResponseText,
                RoomChatMessageKind.User,
                cancellationToken);
            await _roomRuntimeCoordinator.SignalMutationAsync(
                roomId,
                RoomRuntimeMutationKind.ChatMessageAppended,
                cancellationToken);
            await _hotelEventBus.PublishAsync(
                HotelEventKind.BotDialoguePublished,
                new ChatMessagePublishedEvent(
                    roomId,
                    botActor.ActorId,
                    botActor.DisplayName,
                    RoomChatMessageKind.User,
                    reply.ResponseText),
                null,
                roomId,
                cancellationToken);

            return botMessage;
        }

        return null;
    }

    private async ValueTask<IReadOnlyList<HotelBotDefinition>> GetRoomBotDefinitionsAsync(
        RoomId roomId,
        NavigatorPublicRoomDefinition? publicRoomEntry,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<HotelBotDefinition> roomBots =
            await _botDefinitionRepository.GetByRoomIdAsync(roomId, cancellationToken);
        IReadOnlyList<HotelBotDefinition> packageBots =
            publicRoomEntry is null
                ? []
                : await _botDefinitionRepository.GetByAssetPackageKeyAsync(publicRoomEntry.AssetPackageKey, cancellationToken);

        return roomBots
            .Concat(packageBots)
            .GroupBy(bot => bot.BotKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .OrderBy(bot => bot.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static RoomActorState BuildBotActorState(
        RoomId roomId,
        RoomLayoutDefinition roomLayout,
        HotelBotDefinition bot,
        long actorId)
    {
        int spawnX = roomLayout.DoorPosition.X + bot.SpawnOffsetX;
        int spawnY = roomLayout.DoorPosition.Y + bot.SpawnOffsetY;
        double spawnZ = roomLayout.DoorPosition.Z;

        // HOTFIX patrol: PendingSteps left empty — the tick scheduler computes the
        // actual A* path on the first tick.  PatrolWaypoints embeds the full route
        // so the scheduler can cycle bots indefinitely without re-reading definitions.
        MovementGoal? initialGoal = bot.Waypoints.Count > 0
            ? new MovementGoal(bot.Waypoints[0].X, bot.Waypoints[0].Y, [])
              {
                  PatrolWaypoints = bot.Waypoints,
                  PatrolWaypointIndex = 0
              }
            : null;

        return new RoomActorState(
            ActorId: actorId,
            ActorKind: RoomActorKind.Bot,
            DisplayName: bot.DisplayName,
            Position: new RoomCoordinate(spawnX, spawnY, spawnZ),
            BodyRotation: bot.BodyRotation,
            HeadRotation: bot.BodyRotation,
            IsTyping: false,
            IsWalking: false,
            IsSitting: false,
            IsLaying: false,
            CarryItem: null,
            Goal: initialGoal,
            StatusEntries:
            [
                new ActorStatusEntry("bot", bot.DialogueMode.ToString().ToLowerInvariant()),
                new ActorStatusEntry("lang", bot.LanguageCode)
            ]);
    }

    private static long BuildBotActorId(string botKey)
    {
        unchecked
        {
            long hash = 1469598103934665603;
            foreach (char character in botKey)
            {
                hash ^= character;
                hash *= 1099511628211;
            }

            long value = Math.Abs(hash % int.MaxValue);
            return -(value + 10_000);
        }
    }
}
