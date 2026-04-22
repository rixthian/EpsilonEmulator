using Epsilon.Games;

namespace Epsilon.Persistence;

internal sealed class InMemoryGameVenueRepository : IGameVenueRepository
{
    private readonly InMemoryHotelStore _store;

    public InMemoryGameVenueRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<IReadOnlyList<GameVenueDefinition>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<IReadOnlyList<GameVenueDefinition>>(_store.GameVenues.OrderBy(venue => venue.DisplayName).ToArray());
    }
}
