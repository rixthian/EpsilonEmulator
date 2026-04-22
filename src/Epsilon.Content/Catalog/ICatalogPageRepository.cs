using Epsilon.CoreGame;

namespace Epsilon.Content;

public interface ICatalogPageRepository
{
    ValueTask<IReadOnlyList<CatalogPageDefinition>> GetVisibleByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);

    ValueTask<CatalogPageDefinition?> GetByIdAsync(
        CatalogPageId catalogPageId,
        CancellationToken cancellationToken = default);
}
