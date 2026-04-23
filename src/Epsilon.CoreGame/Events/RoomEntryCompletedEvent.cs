namespace Epsilon.CoreGame;

public sealed record RoomEntryCompletedEvent(
    CharacterId CharacterId,
    string Username,
    RoomId RoomId,
    RoomKind RoomKind,
    bool SpectatorMode);
