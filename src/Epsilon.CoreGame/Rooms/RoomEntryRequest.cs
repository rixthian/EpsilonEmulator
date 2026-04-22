namespace Epsilon.CoreGame;

public sealed record RoomEntryRequest(
    CharacterId CharacterId,
    RoomId RoomId,
    string? Password,
    bool SpectatorMode);
