namespace Epsilon.CoreGame;

public sealed record RoomMoveInput(
    long RoomId,
    int DestinationX,
    int DestinationY);
