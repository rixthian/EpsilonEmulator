namespace Epsilon.CoreGame;

public sealed record RoomChatRequest(
    CharacterId CharacterId,
    RoomId RoomId,
    string Message);
