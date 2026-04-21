namespace Epsilon.CoreGame;

public sealed record RoomActivitySnapshot(
    RoomActivityKind ActivityKind,
    bool IsActive,
    string PhaseCode,
    IReadOnlyList<string> TeamCodes,
    IReadOnlyDictionary<string, int> Scoreboard);
