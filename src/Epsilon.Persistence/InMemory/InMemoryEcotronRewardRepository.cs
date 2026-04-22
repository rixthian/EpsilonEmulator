using Epsilon.Content;

namespace Epsilon.Persistence;

internal sealed class InMemoryEcotronRewardRepository : IEcotronRewardRepository
{
    private readonly InMemoryHotelStore _store;

    public InMemoryEcotronRewardRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<IReadOnlyList<EcotronRewardDefinition>> GetVisibleAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<EcotronRewardDefinition> rewards = _store.EcotronRewards
            .Where(candidate => candidate.IsVisibleInCatalog)
            .OrderBy(candidate => candidate.RewardTier)
            .ThenBy(candidate => candidate.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return ValueTask.FromResult(rewards);
    }
}
