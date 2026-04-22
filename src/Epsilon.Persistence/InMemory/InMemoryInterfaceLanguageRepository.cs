using Epsilon.Content;

namespace Epsilon.Persistence;

internal sealed class InMemoryInterfaceLanguageRepository : IInterfaceLanguageRepository
{
    private readonly InMemoryHotelStore _store;

    public InMemoryInterfaceLanguageRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<IReadOnlyList<InterfaceLanguageDefinition>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<IReadOnlyList<InterfaceLanguageDefinition>>(_store.InterfaceLanguages);
    }

    public ValueTask<InterfaceLanguageDefinition?> GetByLanguageCodeAsync(
        string languageCode,
        CancellationToken cancellationToken = default)
    {
        InterfaceLanguageDefinition? language = _store.InterfaceLanguages
            .FirstOrDefault(candidate => string.Equals(candidate.LanguageCode, languageCode, StringComparison.OrdinalIgnoreCase));
        return ValueTask.FromResult(language);
    }
}
