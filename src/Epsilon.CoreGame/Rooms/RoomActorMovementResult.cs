namespace Epsilon.CoreGame;

public sealed record RoomActorMovementResult(
    bool Succeeded,
    string Detail,
    RoomActorState? ActorState);
