namespace Epsilon.Content;

public interface IEcotronRewardRepository
{
    ValueTask<IReadOnlyList<EcotronRewardDefinition>> GetVisibleAsync(
        CancellationToken cancellationToken = default);
}
