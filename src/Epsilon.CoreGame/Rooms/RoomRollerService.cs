using Epsilon.Content;
using Epsilon.Rooms;

namespace Epsilon.CoreGame;

public sealed class RoomRollerService : IRoomRollerService
{
    private readonly IHotelReadService _hotelReadService;
    private readonly IRoomRuntimeRepository _roomRuntimeRepository;
    private readonly IRoomRuntimeCoordinator _roomRuntimeCoordinator;
    private readonly IRoomItemRepository _roomItemRepository;
    private readonly IHotelEventBus _hotelEventBus;

    public RoomRollerService(
        IHotelReadService hotelReadService,
        IRoomRuntimeRepository roomRuntimeRepository,
        IRoomRuntimeCoordinator roomRuntimeCoordinator,
        IRoomItemRepository roomItemRepository,
        IHotelEventBus hotelEventBus)
    {
        _hotelReadService = hotelReadService;
        _roomRuntimeRepository = roomRuntimeRepository;
        _roomRuntimeCoordinator = roomRuntimeCoordinator;
        _roomItemRepository = roomItemRepository;
        _hotelEventBus = hotelEventBus;
    }

    public async ValueTask<int> ProcessAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        RoomHotelSnapshot? room = await _hotelReadService.GetRoomSnapshotAsync(roomId, cancellationToken);
        if (room?.Layout is null)
        {
            return 0;
        }

        IReadOnlyList<RoomActorState> actors = await _roomRuntimeRepository.GetActorsByRoomIdAsync(roomId, cancellationToken);
        if (actors.Count == 0)
        {
            return 0;
        }

        Dictionary<(RoomActorKind Kind, long ActorId), RoomActorState> workingActors = actors
            .GroupBy(actor => (actor.ActorKind, actor.ActorId))
            .ToDictionary(group => group.Key, group => group.Last());
        Dictionary<ItemId, RoomItemSnapshot> workingItems = room.Items
            .GroupBy(snapshot => snapshot.Item.ItemId)
            .ToDictionary(group => group.Key, group => group.Last());
        HashSet<(RoomActorKind Kind, long ActorId)> processedActors = [];
        HashSet<ItemId> processedItems = [];
        List<(RoomActorState Previous, RoomActorState Updated)> actorChanges = [];
        bool itemChanged = false;

        IReadOnlyList<RoomItemSnapshot> rollers = room.Items
            .Where(snapshot =>
                snapshot.Item.Placement.FloorPosition is not null &&
                string.Equals(snapshot.Definition?.InteractionTypeCode, "roller", StringComparison.OrdinalIgnoreCase))
            .OrderBy(snapshot => snapshot.Item.ItemId.Value)
            .ToArray();

        foreach (RoomItemSnapshot roller in rollers)
        {
            FloorPosition rollerPosition = roller.Item.Placement.FloorPosition!.Value;
            if (!RoomNavigationLogic.TryResolveRollerStep(roller.Item.Placement.Rotation, out int deltaX, out int deltaY))
            {
                continue;
            }

            int targetX = rollerPosition.X + deltaX;
            int targetY = rollerPosition.Y + deltaY;
            if (!RoomNavigationLogic.IsWalkable(room.Layout, targetX, targetY, out double floorHeight))
            {
                continue;
            }

            foreach (RoomActorState actor in workingActors.Values
                         .Where(candidate =>
                             !processedActors.Contains((candidate.ActorKind, candidate.ActorId)) &&
                             candidate.Position.X == rollerPosition.X &&
                             candidate.Position.Y == rollerPosition.Y)
                         .OrderBy(candidate => candidate.ActorId)
                         .ToArray())
            {
                if (RoomNavigationLogic.IsTileBlocked(
                        workingActors.Values.ToArray(),
                        workingItems,
                        actor.ActorKind,
                        actor.ActorId,
                        targetX,
                        targetY))
                {
                    continue;
                }

                double targetHeight = RoomNavigationLogic.ResolveStandingHeight(targetX, targetY, floorHeight, workingItems.Values);
                RoomCoordinate targetPosition = new(targetX, targetY, targetHeight);
                int rotation = RoomNavigationLogic.ResolveMovementRotation(actor.Position, targetPosition);
                IReadOnlyList<ActorStatusEntry> statusEntries = actor.StatusEntries
                    .Where(entry =>
                        !string.Equals(entry.Key, "sit", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(entry.Key, "lay", StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                RoomActorState updatedActor = actor with
                {
                    Position = targetPosition,
                    Goal = null,
                    IsWalking = false,
                    IsSitting = false,
                    IsLaying = false,
                    BodyRotation = rotation,
                    HeadRotation = rotation,
                    StatusEntries = statusEntries
                };

                workingActors[(actor.ActorKind, actor.ActorId)] = updatedActor;
                processedActors.Add((actor.ActorKind, actor.ActorId));
                actorChanges.Add((actor, updatedActor));
            }

            RoomItemSnapshot? itemToMove = workingItems.Values
                .Where(snapshot =>
                    !processedItems.Contains(snapshot.Item.ItemId) &&
                    snapshot.Item.ItemId != roller.Item.ItemId &&
                    snapshot.Item.Placement.FloorPosition is { } floorPosition &&
                    floorPosition.X == rollerPosition.X &&
                    floorPosition.Y == rollerPosition.Y)
                .OrderByDescending(snapshot => snapshot.Item.Placement.FloorPosition!.Value.Z)
                .ThenByDescending(snapshot => snapshot.Definition?.StackHeight ?? 0)
                .FirstOrDefault();

            if (itemToMove is null)
            {
                continue;
            }

            if (!CanMoveItemToTile(targetX, targetY, floorHeight, workingActors.Values, workingItems, itemToMove.Item.ItemId, out double itemTargetHeight))
            {
                continue;
            }

            RoomItemState updatedItem = itemToMove.Item with
            {
                Placement = itemToMove.Item.Placement with
                {
                    FloorPosition = new FloorPosition(targetX, targetY, itemTargetHeight)
                }
            };

            workingItems[itemToMove.Item.ItemId] = itemToMove with { Item = updatedItem };
            processedItems.Add(itemToMove.Item.ItemId);
            itemChanged = true;
        }

        foreach ((RoomActorState previous, RoomActorState updated) in actorChanges)
        {
            await _roomRuntimeRepository.StoreActorStateAsync(roomId, updated, cancellationToken);

            if (updated.ActorKind == RoomActorKind.Player)
            {
                await _hotelEventBus.PublishAsync(
                    HotelEventKind.RoomActorMoved,
                    new RoomActorMovedEvent(
                        new CharacterId(updated.ActorId),
                        updated.DisplayName,
                        roomId,
                        previous.Position,
                        updated.Position),
                    new CharacterId(updated.ActorId),
                    roomId,
                    cancellationToken);
            }
        }

        int mutations = 0;
        if (actorChanges.Count > 0)
        {
            await _roomRuntimeCoordinator.SignalMutationAsync(
                roomId,
                RoomRuntimeMutationKind.ActorStateChanged,
                cancellationToken);
            mutations += actorChanges.Count;
        }

        if (itemChanged)
        {
            await _roomItemRepository.StoreByRoomIdAsync(
                roomId,
                workingItems.Values.Select(snapshot => snapshot.Item).OrderBy(item => item.ItemId.Value).ToArray(),
                cancellationToken);
            await _roomRuntimeCoordinator.SignalMutationAsync(
                roomId,
                RoomRuntimeMutationKind.RoomContentChanged,
                cancellationToken);
            mutations++;
        }

        return mutations;
    }

    private static bool CanMoveItemToTile(
        int x,
        int y,
        double floorHeight,
        IEnumerable<RoomActorState> actors,
        IReadOnlyDictionary<ItemId, RoomItemSnapshot> items,
        ItemId movingItemId,
        out double targetHeight)
    {
        targetHeight = floorHeight;

        if (actors.Any(actor => actor.Position.X == x && actor.Position.Y == y))
        {
            return false;
        }

        List<RoomItemSnapshot> targetItems = items.Values
            .Where(snapshot =>
                snapshot.Item.ItemId != movingItemId &&
                snapshot.Item.Placement.FloorPosition is { } floorPosition &&
                floorPosition.X == x &&
                floorPosition.Y == y)
            .OrderBy(snapshot => snapshot.Item.Placement.FloorPosition!.Value.Z)
            .ToList();

        if (targetItems.Count == 0)
        {
            return true;
        }

        RoomItemSnapshot topItem = targetItems[^1];
        if (!(topItem.Definition?.CanStack ?? false))
        {
            return false;
        }

        FloorPosition topPosition = topItem.Item.Placement.FloorPosition!.Value;
        targetHeight = topPosition.Z + (topItem.Definition?.StackHeight ?? 0);
        return true;
    }
}
