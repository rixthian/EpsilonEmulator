namespace Epsilon.Content;

public interface IClientPackageRepository
{
    ValueTask<ClientPackageManifest?> GetByKeyAsync(
        string packageKey,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<ClientPackageManifest>> GetAllAsync(
        CancellationToken cancellationToken = default);
}
