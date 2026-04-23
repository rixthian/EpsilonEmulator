using Epsilon.Content;

namespace Epsilon.CoreGame;

public sealed class RoomAnimService : IRoomAnimService
{
    private static readonly IReadOnlySet<string> AlwaysAnimatedInteractions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "teleport",
        "gate",
        "onewaygate",
        "switch",
        "vendingmachine",
        "dice",
        "bottle",
        "habbowheel",
        "loveshuffler",
        "puzzlebox",
        "scoreboard",
        "roomeffect",
        "dimmer",
        "water",
        "roller",
        "ball",
        "counter"
    };

    private readonly IHotelReadService _hotelReadService;
    private readonly IRoomRuntimeRepository _roomRuntimeRepository;

    public RoomAnimService(
        IHotelReadService hotelReadService,
        IRoomRuntimeRepository roomRuntimeRepository)
    {
        _hotelReadService = hotelReadService;
        _roomRuntimeRepository = roomRuntimeRepository;
    }

    public async ValueTask<RoomAnimSnapshot?> BuildAsync(
        RoomId roomId,
        CancellationToken cancellationToken = default)
    {
        RoomHotelSnapshot? room = await _hotelReadService.GetRoomSnapshotAsync(roomId, cancellationToken);
        if (room is null)
        {
            return null;
        }

        IReadOnlyList<RoomActorState> actors = await _roomRuntimeRepository.GetActorsByRoomIdAsync(roomId, cancellationToken);

        ActorAnimState[] actorStates = actors
            .Select(MapActorAnimation)
            .OrderBy(actor => actor.ActorId)
            .ToArray();

        ItemAnimState[] itemStates = room.Items
            .Select(MapItemAnimation)
            .OrderBy(item => item.ItemId)
            .ToArray();

        return new RoomAnimSnapshot(room.Room.RoomId, room.Room.Name, actorStates, itemStates);
    }

    private static ActorAnimState MapActorAnimation(RoomActorState actor)
    {
        Dictionary<string, string> statuses = actor.StatusEntries
            .GroupBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last().Value, StringComparer.OrdinalIgnoreCase);

        List<string> layers = [];
        string? carryItemName = null;
        if (actor.CarryItem is { ExpiresAtUtc: var expiresAtUtc } carryItem && expiresAtUtc > DateTime.UtcNow)
        {
            carryItemName = carryItem.DisplayName;
            layers.Add("carry");
        }

        if (actor.IsTyping)
        {
            layers.Add("typing");
        }

        if (statuses.ContainsKey("sign"))
        {
            layers.Add("sign");
        }

        if (statuses.ContainsKey("dance"))
        {
            return new ActorAnimState(actor.ActorId, actor.ActorKind, actor.DisplayName, "dance", statuses["dance"], layers, carryItemName);
        }

        if (statuses.ContainsKey("gest"))
        {
            return new ActorAnimState(actor.ActorId, actor.ActorKind, actor.DisplayName, "gesture", statuses["gest"], layers, carryItemName);
        }

        if (statuses.TryGetValue("wav", out string? waveValue) && string.Equals(waveValue, "1", StringComparison.OrdinalIgnoreCase))
        {
            return new ActorAnimState(actor.ActorId, actor.ActorKind, actor.DisplayName, "wave", "1", layers, carryItemName);
        }

        if (actor.IsWalking || actor.Goal is not null)
        {
            return new ActorAnimState(actor.ActorId, actor.ActorKind, actor.DisplayName, "walk", null, layers, carryItemName);
        }

        if (actor.IsLaying || statuses.ContainsKey("lay"))
        {
            return new ActorAnimState(actor.ActorId, actor.ActorKind, actor.DisplayName, "lay", statuses.GetValueOrDefault("lay"), layers, carryItemName);
        }

        if (actor.IsSitting || statuses.ContainsKey("sit"))
        {
            return new ActorAnimState(actor.ActorId, actor.ActorKind, actor.DisplayName, "sit", statuses.GetValueOrDefault("sit"), layers, carryItemName);
        }

        if (statuses.ContainsKey("idle"))
        {
            return new ActorAnimState(actor.ActorId, actor.ActorKind, actor.DisplayName, "idle", statuses["idle"], layers, carryItemName);
        }

        return new ActorAnimState(actor.ActorId, actor.ActorKind, actor.DisplayName, "stand", null, layers, carryItemName);
    }

    private static ItemAnimState MapItemAnimation(RoomItemSnapshot item)
    {
        string interactionType = item.Definition?.InteractionTypeCode ?? "default";
        string publicName = item.Definition?.PublicName ?? item.Item.ItemId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string stateData = item.Item.StateData.Trim();
        bool hasActiveState = !string.IsNullOrWhiteSpace(stateData) && !string.Equals(stateData, "0", StringComparison.OrdinalIgnoreCase);
        bool isAnimated = hasActiveState || AlwaysAnimatedInteractions.Contains(interactionType);

        (string animationKey, bool isLooping) = interactionType.ToLowerInvariant() switch
        {
            "gate" or "onewaygate" => (hasActiveState ? "open" : "closed", false),
            "switch" => (hasActiveState ? "on" : "off", false),
            "teleport" => (hasActiveState ? "charged" : "idle", true),
            "vendingmachine" => (hasActiveState ? "dispense" : "idle", false),
            "dice" or "bottle" or "habbowheel" or "loveshuffler" or "puzzlebox" => (hasActiveState ? "rolling" : "idle", false),
            "scoreboard" => (hasActiveState ? "score" : "idle", true),
            "roomeffect" or "dimmer" or "water" or "roller" or "ball" or "counter" => (hasActiveState ? "active" : "idle", true),
            _ => (hasActiveState ? "active" : "static", false)
        };

        return new ItemAnimState(
            item.Item.ItemId.Value,
            publicName,
            interactionType,
            animationKey,
            stateData,
            isAnimated,
            isLooping);
    }
}
