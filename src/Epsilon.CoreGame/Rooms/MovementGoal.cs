namespace Epsilon.CoreGame;

// HOTFIX: MovementGoal now stores the full A* path so the tick scheduler can
// advance the actor one step at a time instead of teleporting to the destination.
// HOTFIX patrol: PatrolWaypoints / PatrolWaypointIndex are init-only so all
// existing 3-arg constructors continue to compile unchanged.  The tick scheduler
// uses them to cycle bots through their waypoint list after each arrival.
public sealed record MovementGoal(
    int DestinationX,
    int DestinationY,
    IReadOnlyList<RoomCoordinate> PendingSteps)
{
    /// <summary>
    /// Non-null only for bot actors that have a patrol route.
    /// The scheduler cycles through this list indefinitely.
    /// </summary>
    public IReadOnlyList<BotWaypoint>? PatrolWaypoints { get; init; }

    /// <summary>
    /// Index into <see cref="PatrolWaypoints"/> that this goal is currently
    /// heading toward.  After arrival the scheduler increments (mod Count) and
    /// queues a new goal for the next waypoint.
    /// </summary>
    public int PatrolWaypointIndex { get; init; }
}
