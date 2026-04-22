namespace Epsilon.CoreGame;

public sealed record HotelBotDefinition(
    string BotKey,
    string AssetPackageKey,
    string DisplayName,
    string LanguageCode,
    BotDialogueMode DialogueMode,
    string GreetingMessage,
    string IdleMessage,
    int SpawnOffsetX,
    int SpawnOffsetY,
    int BodyRotation,
    bool IsEnabled,
    IReadOnlyList<string> AllowedTopics,
    IReadOnlyList<BotWaypoint> Waypoints,
    IReadOnlyList<BotReplyDefinition> Replies);
