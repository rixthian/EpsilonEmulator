namespace Epsilon.CoreGame;

public sealed class RoomTickScheduler : IRoomTickScheduler
{
    private readonly IRoomRuntimeRepository _roomRuntimeRepository;
    private readonly IRoomRollerService _roomRollerService;
    // HOTFIX: Injected so movement advancement can signal mutations after each tick.
    private readonly IRoomRuntimeCoordinator _roomRuntimeCoordinator;
    // HOTFIX patrol: needed to load room snapshot for on-demand A* path computation
    // when a bot's PendingSteps list is empty but Goal is still set (first tick after
    // spawn, or after patrol waypoint cycling resets the path).
    private readonly IHotelReadService _hotelReadService;
    private long _tickCounter;

    public RoomTickScheduler(
        IRoomRuntimeRepository roomRuntimeRepository,
        IRoomRollerService roomRollerService,
        IRoomRuntimeCoordinator roomRuntimeCoordinator,
        IHotelReadService hotelReadService)
    {
        _roomRuntimeRepository = roomRuntimeRepository;
        _roomRollerService = roomRollerService;
        _roomRuntimeCoordinator = roomRuntimeCoordinator;
        _hotelReadService = hotelReadService;
    }

    public async ValueTask<int> TickAsync(int rollerIntervalTicks, CancellationToken cancellationToken = default)
    {
        if (rollerIntervalTicks <= 0)
            return 0;

        long tick = Interlocked.Increment(ref _tickCounter);
        bool runRollers = tick % rollerIntervalTicks == 0;

        IReadOnlyList<RoomId> activeRoomIds = await _roomRuntimeRepository.GetAllActiveRoomIdsAsync(cancellationToken);
        if (activeRoomIds.Count == 0)
            return 0;

        // HOTFIX parallel: Process every active room concurrently instead of sequentially.
        // Sequential processing caused the game loop to fall behind under load: N rooms × T ms
        // per room compounded until ticks arrived late and movement/rollers visibly stuttered.
        int[] perRoomMutations = await Task.WhenAll(activeRoomIds.Select(roomId =>
            ProcessRoomTickAsync(roomId, runRollers, cancellationToken)));

        return perRoomMutations.Sum();
    }

    private async Task<int> ProcessRoomTickAsync(
        RoomId roomId,
        bool runRollers,
        CancellationToken cancellationToken)
    {
        // HOTFIX: Movement advancement runs every tick so actors walk smoothly.
        // Rollers run on their sub-interval (default: every 6 ticks = ~2.5 s).
        int mutations = await AdvanceMovementStepsAsync(roomId, cancellationToken);
        if (runRollers)
            mutations += await _roomRollerService.ProcessAsync(roomId, cancellationToken);
        return mutations;
    }

    // HOTFIX step-by-step movement: advances each walking actor exactly one tile
    // per tick.  MoveActorAsync now stores the full A* path in actor.Goal.PendingSteps
    // and marks IsWalking = true.  This method pops the next step, updates position,
    // and clears the Goal when the actor reaches the destination.
    //
    // HOTFIX bot patrol: actors with Goal != null but PendingSteps.Count == 0 need
    // their A* path computed here (first tick after spawn or after waypoint cycling).
    // When a patrolling bot arrives it is automatically queued toward the next waypoint.
    private async Task<int> AdvanceMovementStepsAsync(RoomId roomId, CancellationToken cancellationToken)
    {
        IReadOnlyList<RoomActorState> actors =
            await _roomRuntimeRepository.GetActorsByRoomIdAsync(roomId, cancellationToken);

        // Actors that need a path computed this tick (Goal set but PendingSteps empty).
        RoomActorState[] pathPendingActors = actors
            .Where(static a => a.Goal is { PendingSteps.Count: 0 })
            .ToArray();

        // Actors already mid-walk (Goal set with at least one pending step).
        RoomActorState[] walkingActors = actors
            .Where(static a => a.Goal is { PendingSteps.Count: > 0 })
            .ToArray();

        if (pathPendingActors.Length == 0 && walkingActors.Length == 0)
            return 0;

        // Lazy-load the room snapshot only when there are path-pending actors that
        // require A* computation.  Avoids unnecessary reads for rooms with only
        // already-pathed walking actors.
        RoomHotelSnapshot? room = null;
        string[]? heightmapRows = null;
        IReadOnlyList<RoomActorState> allActors = actors;

        if (pathPendingActors.Length > 0)
        {
            room = await _hotelReadService.GetRoomSnapshotAsync(roomId, cancellationToken);
            if (room?.Layout is not null)
            {
                heightmapRows = RoomNavigationLogic.SplitHeightmap(room.Layout.Heightmap);
            }
        }

        int mutations = 0;

        // Phase 1: compute A* paths for actors whose Goal has empty PendingSteps.
        // This covers bots on their first tick after spawn and after patrol cycling.
        if (room is not null && heightmapRows is not null)
        {
            Dictionary<ItemId, RoomItemSnapshot> itemSnapshots = room.Items
                .GroupBy(snapshot => snapshot.Item.ItemId)
                .ToDictionary(group => group.Key, group => group.Last());

            foreach (RoomActorState actor in pathPendingActors)
            {
                MovementGoal goal = actor.Goal!;

                IReadOnlyList<RoomCoordinate>? path = RoomNavigationLogic.FindPath(
                    room, allActors, actor, goal.DestinationX, goal.DestinationY, itemSnapshots);

                if (path is null || path.Count <= 1)
                {
                    // Destination unreachable — clear the goal so the bot doesn't spin.
                    RoomActorState cleared = actor with { Goal = null, IsWalking = false };
                    await _roomRuntimeRepository.StoreActorStateAsync(roomId, cleared, cancellationToken);
                    mutations++;
                    continue;
                }

                IReadOnlyList<RoomCoordinate> pendingSteps = path.Skip(1).ToArray();

                RoomActorState pathed = actor with
                {
                    Goal = goal with { PendingSteps = pendingSteps },
                    IsWalking = true,
                    IsSitting = false,
                    IsLaying = false
                };

                await _roomRuntimeRepository.StoreActorStateAsync(roomId, pathed, cancellationToken);
                mutations++;

                // Replace entry in the in-memory snapshot so Phase 2 sees updated state.
                // (walkingActors was already snapshotted — pathed actors start moving next tick.)
            }
        }

        // Phase 2: advance each already-pathed walking actor by exactly one tile.
        foreach (RoomActorState actor in walkingActors)
        {
            MovementGoal goal = actor.Goal!;
            RoomCoordinate nextStep = goal.PendingSteps[0];
            IReadOnlyList<RoomCoordinate> remaining = goal.PendingSteps.Count > 1
                ? goal.PendingSteps.Skip(1).ToArray()
                : (IReadOnlyList<RoomCoordinate>)[];

            bool arrived = remaining.Count == 0;
            int rotation = RoomNavigationLogic.ResolveMovementRotation(actor.Position, nextStep);

            // Strip posture status entries while walking.
            IReadOnlyList<ActorStatusEntry> statusEntries = actor.StatusEntries
                .Where(static e =>
                    !string.Equals(e.Key, "sit", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(e.Key, "lay", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            // HOTFIX patrol: when a patrolling bot arrives at its waypoint, queue the
            // next waypoint goal with empty PendingSteps so Phase 1 computes its path
            // on the following tick (after the optional pause, which we don't have a
            // timer for yet — the bot simply starts moving toward the next point
            // immediately, which is the correct observable behavior).
            MovementGoal? nextGoal;
            if (arrived && goal.PatrolWaypoints is { Count: > 0 } patrol)
            {
                int nextIndex = (goal.PatrolWaypointIndex + 1) % patrol.Count;
                BotWaypoint nextWaypoint = patrol[nextIndex];
                nextGoal = new MovementGoal(nextWaypoint.X, nextWaypoint.Y, [])
                {
                    PatrolWaypoints = patrol,
                    PatrolWaypointIndex = nextIndex
                };
            }
            else
            {
                nextGoal = arrived ? null : goal with { PendingSteps = remaining };
            }

            RoomActorState updated = actor with
            {
                Position = nextStep,
                Goal = nextGoal,
                IsWalking = nextGoal is not null,
                IsSitting = arrived && actor.IsSitting && nextGoal is null,
                IsLaying = arrived && actor.IsLaying && nextGoal is null,
                BodyRotation = rotation,
                HeadRotation = rotation,
                StatusEntries = statusEntries
            };

            await _roomRuntimeRepository.StoreActorStateAsync(roomId, updated, cancellationToken);
            mutations++;
        }

        if (mutations > 0)
        {
            await _roomRuntimeCoordinator.SignalMutationAsync(
                roomId,
                RoomRuntimeMutationKind.ActorStateChanged,
                cancellationToken);
        }

        return mutations;
    }
}
