using Epsilon.CoreGame;

namespace Epsilon.Content;

public interface ICatalogCampaignRepository
{
    ValueTask<IReadOnlyList<CatalogCampaignDefinition>> GetVisibleByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);
}
