namespace Epsilon.Content;

public sealed record InterfaceLanguageDefinition(
    string LanguageCode,
    string CultureCode,
    string DisplayName,
    string NativeDisplayName,
    string IconAssetPath,
    bool IsDefault,
    int SortOrder);
