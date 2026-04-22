namespace Epsilon.Content;

public interface ICollectibleRepository
{
    ValueTask<IReadOnlyList<CollectibleDefinition>> GetVisibleAsync(
        CancellationToken cancellationToken = default);
}
