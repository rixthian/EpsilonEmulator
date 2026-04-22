using Epsilon.Content;
using Epsilon.Rooms;

namespace Epsilon.CoreGame;

public sealed class RoomBotRuntimeService : IRoomBotRuntimeService
{
    private readonly IRoomBotDefinitionRepository _botDefinitionRepository;
    private readonly INavigatorPublicRoomRepository _navigatorPublicRoomRepository;
    private readonly IRoomRuntimeRepository _roomRuntimeRepository;
    private readonly IRoomRuntimeCoordinator _roomRuntimeCoordinator;

    public RoomBotRuntimeService(
        IRoomBotDefinitionRepository botDefinitionRepository,
        INavigatorPublicRoomRepository navigatorPublicRoomRepository,
        IRoomRuntimeRepository roomRuntimeRepository,
        IRoomRuntimeCoordinator roomRuntimeCoordinator)
    {
        _botDefinitionRepository = botDefinitionRepository;
        _navigatorPublicRoomRepository = navigatorPublicRoomRepository;
        _roomRuntimeRepository = roomRuntimeRepository;
        _roomRuntimeCoordinator = roomRuntimeCoordinator;
    }

    public async ValueTask<IReadOnlyList<RoomActorState>> EnsurePublicRoomBotsAsync(
        NavigatorPublicRoomDefinition publicRoomEntry,
        RoomLayoutDefinition roomLayout,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<HotelBotDefinition> botDefinitions =
            await _botDefinitionRepository.GetByAssetPackageKeyAsync(publicRoomEntry.AssetPackageKey, cancellationToken);

        if (botDefinitions.Count == 0)
        {
            return [];
        }

        List<RoomActorState> materializedBots = [];
        bool anyCreated = false;
        foreach (HotelBotDefinition bot in botDefinitions.Where(static bot => bot.IsEnabled))
        {
            long actorId = BuildBotActorId(bot.BotKey);
            RoomActorState? existingActor = await _roomRuntimeRepository.GetActorByIdAsync(
                publicRoomEntry.RoomId,
                actorId,
                cancellationToken);

            if (existingActor is not null)
            {
                materializedBots.Add(existingActor);
                continue;
            }

            RoomActorState botActor = BuildBotActorState(publicRoomEntry.RoomId, roomLayout, bot, actorId);
            await _roomRuntimeRepository.StoreActorStateAsync(publicRoomEntry.RoomId, botActor, cancellationToken);
            materializedBots.Add(botActor);
            anyCreated = true;
        }

        if (anyCreated)
        {
            await _roomRuntimeCoordinator.SignalMutationAsync(
                publicRoomEntry.RoomId,
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
        if (publicRoomEntry is null)
        {
            return null;
        }

        IReadOnlyList<HotelBotDefinition> botDefinitions =
            await _botDefinitionRepository.GetByAssetPackageKeyAsync(publicRoomEntry.AssetPackageKey, cancellationToken);
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

            return botMessage;
        }

        return null;
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

        MovementGoal? initialGoal = bot.Waypoints.Count > 0
            ? new MovementGoal(bot.Waypoints[0].X, bot.Waypoints[0].Y)
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
