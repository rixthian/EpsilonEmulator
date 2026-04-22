namespace Epsilon.CoreGame;

public sealed record RoomChatResult(
    bool Succeeded,
    string Detail,
    bool IsCommand,
    string? ExecutedCommandKey,
    RoomChatMessage? Message);
