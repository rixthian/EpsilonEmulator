namespace Epsilon.Content;

public interface ILocalizedTextBundleRepository
{
    ValueTask<LocalizedTextBundle?> GetByKeyAsync(
        string bundleKey,
        string languageCode,
        CancellationToken cancellationToken = default);
}
