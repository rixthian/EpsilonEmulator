using Epsilon.CoreGame;

namespace Epsilon.Games;

public sealed record GameSessionState(
    string SessionKey,
    string GameKey,
    string VenueKey,
    RoomId RoomId,
    GameSessionStatus Status,
    string PhaseCode,
    bool IsPrivateMatch,
    int MaximumPlayers,
    DateTime StartedAtUtc,
    IReadOnlyList<GameTeamDefinition> Teams,
    IReadOnlyList<GamePlayerState> Players);
