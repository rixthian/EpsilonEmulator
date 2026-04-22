using Epsilon.Content;
using Epsilon.CoreGame;

namespace Epsilon.Persistence;

internal sealed class InMemoryCatalogCampaignRepository : ICatalogCampaignRepository
{
    private readonly InMemoryHotelStore _store;

    public InMemoryCatalogCampaignRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<IReadOnlyList<CatalogCampaignDefinition>> GetVisibleByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CatalogCampaignDefinition> campaigns = _store.CatalogCampaigns
            .Where(candidate => candidate.IsVisible)
            .OrderBy(candidate => candidate.OrderNumber)
            .ToArray();

        return ValueTask.FromResult(campaigns);
    }
}
