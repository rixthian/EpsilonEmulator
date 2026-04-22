using Epsilon.Content;

namespace Epsilon.CoreGame;

public sealed record CatalogCampaignSnapshot(
    CatalogCampaignDefinition Campaign,
    CatalogPageSnapshot? Page);

public sealed record CatalogLandingSnapshot(
    IReadOnlyList<CatalogCampaignSnapshot> Campaigns,
    CatalogOfferSnapshot? RareOfTheWeek,
    CatalogPageSnapshot? EcotronPage,
    IReadOnlyList<EffectDefinition> FeaturedEffects);
