using Epsilon.CoreGame;

namespace Epsilon.Content;

public sealed record CatalogPageDefinition(
    CatalogPageId CatalogPageId,
    CatalogPageId? ParentPageId,
    string Caption,
    int IconColor,
    int IconImage,
    bool IsVisible,
    bool IsEnabled,
    int MinimumRank,
    bool ClubOnly,
    int OrderNumber,
    string LayoutCode,
    string Headline,
    string Teaser,
    string SpecialTemplate,
    string TextPrimary,
    string TextSecondary,
    string TextDetails,
    string TextTeaser);

