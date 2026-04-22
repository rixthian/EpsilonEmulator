using Epsilon.Content;

namespace Epsilon.Persistence;

internal sealed class InMemoryCatalogFeatureStateRepository : ICatalogFeatureStateRepository
{
    private readonly InMemoryHotelStore _store;

    public InMemoryCatalogFeatureStateRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<CatalogFeatureState?> GetByFeatureKeyAsync(
        string featureKey,
        CancellationToken cancellationToken = default)
    {
        _store.CatalogFeatureStates.TryGetValue(featureKey, out CatalogFeatureState? state);
        return ValueTask.FromResult(state);
    }

    public ValueTask StoreAsync(
        CatalogFeatureState state,
        CancellationToken cancellationToken = default)
    {
        _store.CatalogFeatureStates[state.FeatureKey] = state;
        return ValueTask.CompletedTask;
    }
}
