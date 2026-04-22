using Epsilon.Content;

namespace Epsilon.CoreGame;

public sealed record InterfacePreferenceSnapshot(
    string SelectedLanguageCode,
    IReadOnlyList<InterfaceLanguageDefinition> SupportedLanguages,
    bool CanChangeInPortal,
    bool CanChangeInClientSettings);
