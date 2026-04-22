namespace Epsilon.CoreGame;

public interface IRoomBotDefinitionRepository
{
    ValueTask<IReadOnlyList<HotelBotDefinition>> GetByAssetPackageKeyAsync(
        string assetPackageKey,
        CancellationToken cancellationToken = default);
}
