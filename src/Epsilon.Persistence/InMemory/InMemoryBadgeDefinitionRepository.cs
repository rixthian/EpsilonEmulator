using Epsilon.Content;

namespace Epsilon.Persistence;

internal sealed class InMemoryBadgeDefinitionRepository : IBadgeDefinitionRepository
{
    private readonly InMemoryHotelStore _store;

    public InMemoryBadgeDefinitionRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<BadgeDefinition?> GetByCodeAsync(
        string badgeCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(badgeCode))
        {
            return ValueTask.FromResult<BadgeDefinition?>(null);
        }

        BadgeDefinition? result = _store.BadgeDefinitions.FirstOrDefault(candidate =>
            string.Equals(candidate.BadgeCode, badgeCode, StringComparison.OrdinalIgnoreCase));

        return ValueTask.FromResult(result);
    }

    public ValueTask<IReadOnlyList<BadgeDefinition>> SearchAsync(
        string? query,
        int take,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<BadgeDefinition> badges = _store.BadgeDefinitions;

        if (!string.IsNullOrWhiteSpace(query))
        {
            string normalizedQuery = query.Trim();
            badges = badges.Where(candidate =>
                candidate.BadgeCode.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) ||
                candidate.BadgeName.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) ||
                (candidate.BadgeGroup?.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        IReadOnlyList<BadgeDefinition> results = badges
            .OrderBy(candidate => candidate.BadgeCode, StringComparer.OrdinalIgnoreCase)
            .Take(take)
            .ToArray();

        return ValueTask.FromResult(results);
    }
}
