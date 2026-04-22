using Epsilon.Content;

namespace Epsilon.CoreGame;

public sealed class BadgeCatalogService : IBadgeCatalogService
{
    private readonly IBadgeDefinitionRepository _badgeDefinitionRepository;

    public BadgeCatalogService(IBadgeDefinitionRepository badgeDefinitionRepository)
    {
        _badgeDefinitionRepository = badgeDefinitionRepository;
    }

    public ValueTask<BadgeDefinition?> GetBadgeAsync(
        string badgeCode,
        CancellationToken cancellationToken = default)
    {
        return _badgeDefinitionRepository.GetByCodeAsync(badgeCode, cancellationToken);
    }

    public ValueTask<IReadOnlyList<BadgeDefinition>> SearchAsync(
        string? query,
        int take,
        CancellationToken cancellationToken = default)
    {
        int boundedTake = Math.Clamp(take <= 0 ? 100 : take, 1, 250);
        return _badgeDefinitionRepository.SearchAsync(query, boundedTake, cancellationToken);
    }
}
