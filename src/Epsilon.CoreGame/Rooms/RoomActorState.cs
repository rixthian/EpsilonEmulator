namespace Epsilon.CoreGame;

public sealed record RoomActorState(
    long ActorId,
    RoomActorKind ActorKind,
    string DisplayName,
    RoomCoordinate Position,
    int BodyRotation,
    int HeadRotation,
    bool IsTyping,
    bool IsWalking,
    bool IsSitting,
    bool IsLaying,
    CarryItemState? CarryItem,
    MovementGoal? Goal,
    IReadOnlyList<ActorStatusEntry> StatusEntries);
