namespace Epsilon.Rooms;

public sealed record RoomItemPlacement(
    FloorPosition? FloorPosition,
    int Rotation,
    WallPosition? WallPosition);

