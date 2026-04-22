namespace Epsilon.CoreGame;

public sealed record RoomEntryResult(
    bool Succeeded,
    RoomEntryFailureCode? FailureCode,
    string? FailureDetail,
    IReadOnlyList<RoomEntryStageSnapshot> Stages,
    RoomEntrySnapshot? Snapshot);
