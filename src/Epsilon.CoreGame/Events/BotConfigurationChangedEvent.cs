namespace Epsilon.CoreGame;

public sealed record BotConfigurationChangedEvent(
    CharacterId? CharacterId,
    RoomId RoomId,
    string BotKey,
    string BotName,
    string ChangeKind,
    string Detail);
