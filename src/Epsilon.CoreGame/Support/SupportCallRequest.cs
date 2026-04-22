namespace Epsilon.CoreGame;

public sealed record SupportCallRequest(
    CharacterId SenderCharacterId,
    int CategoryId,
    string Message,
    CharacterId? ReportedCharacterId,
    RoomId? RoomId);
