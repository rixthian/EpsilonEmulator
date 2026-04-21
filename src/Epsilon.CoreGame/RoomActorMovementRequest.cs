namespace Epsilon.CoreGame;

public sealed record RoomActorMovementRequest(
    CharacterId CharacterId,
    RoomId RoomId,
    int DestinationX,
    int DestinationY);
