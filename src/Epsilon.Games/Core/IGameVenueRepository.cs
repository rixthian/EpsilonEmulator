namespace Epsilon.Games;

public interface IGameVenueRepository
{
    ValueTask<IReadOnlyList<GameVenueDefinition>> GetAllAsync(
        CancellationToken cancellationToken = default);
}
