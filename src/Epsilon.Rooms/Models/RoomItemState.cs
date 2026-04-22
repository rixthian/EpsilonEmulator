using Epsilon.CoreGame;

namespace Epsilon.Rooms;

public sealed record RoomItemState(
    ItemId ItemId,
    ItemDefinitionId ItemDefinitionId,
    RoomId RoomId,
    RoomItemPlacement Placement,
    string StateData);

