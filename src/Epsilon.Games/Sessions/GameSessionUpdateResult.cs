namespace Epsilon.Games;

public sealed record GameSessionUpdateResult(
    bool Succeeded,
    string Detail,
    GameSessionState? Session);
