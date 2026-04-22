using Epsilon.CoreGame;

namespace Epsilon.Rooms;

public sealed record RoomLayoutDefinition(
    string LayoutCode,
    FloorPosition DoorPosition,
    int DoorRotation,
    string Heightmap,
    IReadOnlyList<string> PublicRoomObjectSetCodes,
    bool ClubOnly);

