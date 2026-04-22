using Epsilon.CoreGame;

namespace Epsilon.Content;

public sealed record CatalogCampaignDefinition(
    string CampaignKey,
    CatalogPageId CatalogPageId,
    string CampaignType,
    string Title,
    string Subtitle,
    string ImagePath,
    string BadgeLabel,
    bool IsVisible,
    int OrderNumber);
