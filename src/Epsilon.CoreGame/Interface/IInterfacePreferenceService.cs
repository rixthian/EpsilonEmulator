namespace Epsilon.CoreGame;

public interface IInterfacePreferenceService
{
    ValueTask<InterfacePreferenceSnapshot> GetSnapshotAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);

    ValueTask<InterfacePreferenceSnapshot> SetLanguageAsync(
        CharacterId characterId,
        string languageCode,
        CancellationToken cancellationToken = default);
}
