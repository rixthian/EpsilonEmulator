namespace Epsilon.CoreGame;

public sealed record RoomAnimSnapshot(
    RoomId RoomId,
    string RoomName,
    IReadOnlyList<ActorAnimState> Actors,
    IReadOnlyList<ItemAnimState> Items);
