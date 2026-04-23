namespace Epsilon.CoreGame;

public sealed record ModerationActionExecutedEvent(
    CharacterId ModeratorCharacterId,
    string ActionKey,
    CharacterId? TargetCharacterId,
    string? TargetName,
    RoomId? RoomId,
    string Detail);
