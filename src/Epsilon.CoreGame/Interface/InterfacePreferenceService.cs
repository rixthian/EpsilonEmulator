using Epsilon.Content;

namespace Epsilon.CoreGame;

public sealed class InterfacePreferenceService : IInterfacePreferenceService
{
    private readonly ICharacterPreferenceRepository _characterPreferenceRepository;
    private readonly IInterfaceLanguageRepository _interfaceLanguageRepository;

    public InterfacePreferenceService(
        ICharacterPreferenceRepository characterPreferenceRepository,
        IInterfaceLanguageRepository interfaceLanguageRepository)
    {
        _characterPreferenceRepository = characterPreferenceRepository;
        _interfaceLanguageRepository = interfaceLanguageRepository;
    }

    public async ValueTask<InterfacePreferenceSnapshot> GetSnapshotAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<InterfaceLanguageDefinition> languages =
            await _interfaceLanguageRepository.GetAllAsync(cancellationToken);

        InterfaceLanguageDefinition fallbackLanguage =
            languages.OrderBy(language => language.SortOrder).First(language => language.IsDefault || languages.Count > 0);

        CharacterInterfacePreference? storedPreference =
            await _characterPreferenceRepository.GetByCharacterIdAsync(characterId, cancellationToken);

        string selectedLanguageCode = storedPreference?.LanguageCode ?? fallbackLanguage.LanguageCode;
        if (!languages.Any(language => string.Equals(language.LanguageCode, selectedLanguageCode, StringComparison.OrdinalIgnoreCase)))
        {
            selectedLanguageCode = fallbackLanguage.LanguageCode;
        }

        return new InterfacePreferenceSnapshot(
            SelectedLanguageCode: selectedLanguageCode,
            SupportedLanguages: languages.OrderBy(language => language.SortOrder).ToArray(),
            CanChangeInPortal: true,
            CanChangeInClientSettings: true);
    }

    public async ValueTask<InterfacePreferenceSnapshot> SetLanguageAsync(
        CharacterId characterId,
        string languageCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            throw new InvalidOperationException("Language code is required.");
        }

        InterfaceLanguageDefinition? language =
            await _interfaceLanguageRepository.GetByLanguageCodeAsync(languageCode.Trim(), cancellationToken);
        if (language is null)
        {
            throw new InvalidOperationException($"Language '{languageCode}' is not supported.");
        }

        CharacterInterfacePreference preference = new(
            CharacterId: characterId,
            LanguageCode: language.LanguageCode,
            UpdatedAt: DateTimeOffset.UtcNow,
            UpdatedBy: $"character:{characterId.Value}");

        await _characterPreferenceRepository.StoreAsync(preference, cancellationToken);
        return await GetSnapshotAsync(characterId, cancellationToken);
    }
}
