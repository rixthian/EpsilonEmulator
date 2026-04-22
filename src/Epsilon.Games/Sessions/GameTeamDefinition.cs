namespace Epsilon.Games;

public sealed record GameTeamDefinition(
    string TeamKey,
    string DisplayName,
    string ColorKey,
    int ScoreValue);
