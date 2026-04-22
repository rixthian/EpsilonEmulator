using Epsilon.Content;

namespace Epsilon.CoreGame;

public interface IBadgeCatalogService
{
    ValueTask<BadgeDefinition?> GetBadgeAsync(
        string badgeCode,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<BadgeDefinition>> SearchAsync(
        string? query,
        int take,
        CancellationToken cancellationToken = default);
}
