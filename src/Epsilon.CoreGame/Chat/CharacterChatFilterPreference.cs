namespace Epsilon.CoreGame;

public sealed record CharacterChatFilterPreference(
    CharacterId CharacterId,
    bool MuteBots,
    bool MutePets,
    DateTimeOffset UpdatedAt,
    string UpdatedBy);
