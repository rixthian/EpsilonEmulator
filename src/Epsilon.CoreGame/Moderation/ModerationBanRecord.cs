namespace Epsilon.CoreGame;

public sealed record ModerationBanRecord(
    CharacterId TargetCharacterId,
    CharacterId IssuedByCharacterId,
    DateTime IssuedAtUtc,
    DateTime? ExpiresAtUtc,
    string Reason,
    bool IsPermanent);
