namespace Epsilon.Content;

public interface ICatalogFeatureStateRepository
{
    ValueTask<CatalogFeatureState?> GetByFeatureKeyAsync(
        string featureKey,
        CancellationToken cancellationToken = default);

    ValueTask StoreAsync(
        CatalogFeatureState state,
        CancellationToken cancellationToken = default);
}
