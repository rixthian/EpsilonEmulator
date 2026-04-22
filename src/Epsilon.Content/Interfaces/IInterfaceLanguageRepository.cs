namespace Epsilon.Content;

public interface IInterfaceLanguageRepository
{
    ValueTask<IReadOnlyList<InterfaceLanguageDefinition>> GetAllAsync(
        CancellationToken cancellationToken = default);

    ValueTask<InterfaceLanguageDefinition?> GetByLanguageCodeAsync(
        string languageCode,
        CancellationToken cancellationToken = default);
}
