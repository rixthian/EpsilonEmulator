namespace Epsilon.CoreGame;

public sealed record RoomChatPolicySnapshot(
    bool IsMuted,
    bool UsesChatQueue,
    int MaxMessageLength,
    int FloodWindowSeconds,
    int MaxMessagesPerWindow);
