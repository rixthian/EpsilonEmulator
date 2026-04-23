namespace Epsilon.CoreGame;

public sealed record ChatMessagePublishedEvent(
    RoomId RoomId,
    long SenderActorId,
    string SenderName,
    RoomChatMessageKind MessageKind,
    string Message,
    long? TargetActorId = null,
    string? TargetName = null);
