namespace Epsilon.CoreGame;

public sealed record RoomRuntimeSnapshot(
    RoomHotelSnapshot Room,
    IReadOnlyList<RoomActorState> Actors,
    RoomActivitySnapshot? Activity,
    RoomChatPolicySnapshot ChatPolicy,
    IReadOnlyList<RoomChatMessage> ChatMessages);
