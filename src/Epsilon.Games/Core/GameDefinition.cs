namespace Epsilon.Games;

public sealed record GameDefinition(
    string GameKey,
    GameFamily Family,
    string DisplayName,
    string Description,
    string IconAssetPath,
    bool SupportsPublicVenues,
    bool SupportsPrivateVenues,
    bool IsTeamBased,
    bool IsEnabled,
    IReadOnlyList<string> Tags);
