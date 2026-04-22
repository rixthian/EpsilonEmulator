namespace Epsilon.CoreGame;

public sealed record CharacterInterfacePreference(
    CharacterId CharacterId,
    string LanguageCode,
    DateTimeOffset UpdatedAt,
    string UpdatedBy);
