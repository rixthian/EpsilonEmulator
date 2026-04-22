using Epsilon.Content;

namespace Epsilon.Persistence;

internal sealed class InMemoryClientPackageRepository : IClientPackageRepository
{
    private readonly InMemoryHotelStore _store;

    public InMemoryClientPackageRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<ClientPackageManifest?> GetByKeyAsync(
        string packageKey,
        CancellationToken cancellationToken = default)
    {
        _store.ClientPackages.TryGetValue(packageKey, out ClientPackageManifest? packageManifest);
        return ValueTask.FromResult(packageManifest);
    }

    public ValueTask<IReadOnlyList<ClientPackageManifest>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ClientPackageManifest> packages = _store.ClientPackages.Values
            .OrderBy(packageManifest => packageManifest.PackageKey, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return ValueTask.FromResult(packages);
    }
}
