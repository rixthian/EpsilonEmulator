namespace Epsilon.CoreGame;

public sealed record MessengerRequest(
    long RequestId,
    CharacterId SenderCharacterId,
    string SenderUsername,
    string SenderFigure,
    DateTime CreatedAtUtc);
