namespace Epsilon.CoreGame;

public sealed record SupportCallTicket(
    long TicketId,
    int CategoryId,
    CharacterId SenderCharacterId,
    CharacterId? ReportedCharacterId,
    CharacterId? ModeratorCharacterId,
    RoomId? RoomId,
    string RoomName,
    string Message,
    int Score,
    SupportCallStatus Status,
    DateTime CreatedAtUtc);
