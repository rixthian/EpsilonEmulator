namespace Epsilon.CoreGame;

public sealed record SupportCallInput(
    int CategoryId,
    string Message,
    long? ReportedCharacterId,
    long? RoomId);
