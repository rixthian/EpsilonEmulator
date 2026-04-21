namespace Epsilon.CoreGame;

public sealed record RoomChatMessage(
    long MessageId,
    RoomId RoomId,
    long SenderActorId,
    string SenderName,
    string Message,
    RoomChatMessageKind MessageKind,
    DateTime SentAtUtc);
