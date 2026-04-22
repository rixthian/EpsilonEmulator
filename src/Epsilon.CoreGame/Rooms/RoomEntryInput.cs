namespace Epsilon.CoreGame;

public sealed record RoomEntryInput(
    long RoomId,
    string? Password,
    bool SpectatorMode);
