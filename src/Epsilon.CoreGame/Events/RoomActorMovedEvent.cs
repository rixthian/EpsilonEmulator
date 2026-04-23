namespace Epsilon.CoreGame;

public sealed record RoomActorMovedEvent(
    CharacterId CharacterId,
    string Username,
    RoomId RoomId,
    RoomCoordinate FromPosition,
    RoomCoordinate ToPosition);
