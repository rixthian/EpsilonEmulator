namespace Epsilon.CoreGame;

public sealed record RoomEntryStageSnapshot(
    RoomEntryStage Stage,
    bool Succeeded,
    string Detail);
