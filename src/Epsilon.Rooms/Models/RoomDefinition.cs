using Epsilon.CoreGame;

namespace Epsilon.Rooms;

public sealed record RoomDefinition(
    RoomId RoomId,
    RoomKind RoomKind,
    CharacterId? OwnerCharacterId,
    string Name,
    string Description,
    int CategoryId,
    string LayoutCode,
    RoomSettings Settings,
    IReadOnlyList<string> Tags);

