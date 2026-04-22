namespace Epsilon.Content;

public interface IBadgeDefinitionRepository
{
    ValueTask<BadgeDefinition?> GetByCodeAsync(
        string badgeCode,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<BadgeDefinition>> SearchAsync(
        string? query,
        int take,
        CancellationToken cancellationToken = default);
}
