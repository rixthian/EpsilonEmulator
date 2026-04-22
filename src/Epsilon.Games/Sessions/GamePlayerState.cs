using Epsilon.CoreGame;

namespace Epsilon.Games;

public sealed record GamePlayerState(
    CharacterId CharacterId,
    string DisplayName,
    string? TeamKey,
    int ScoreValue,
    bool IsConnected,
    DateTime JoinedAtUtc);
