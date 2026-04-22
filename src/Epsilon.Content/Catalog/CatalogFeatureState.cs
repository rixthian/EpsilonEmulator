using Epsilon.CoreGame;

namespace Epsilon.Content;

public sealed record CatalogFeatureState(
    string FeatureKey,
    bool IsEnabled,
    CatalogOfferId? FeaturedOfferId,
    DateTimeOffset UpdatedAt,
    string UpdatedBy);
