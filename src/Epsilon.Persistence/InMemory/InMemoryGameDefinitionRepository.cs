using Epsilon.Games;

namespace Epsilon.Persistence;

internal sealed class InMemoryGameDefinitionRepository : IGameDefinitionRepository
{
    private readonly InMemoryHotelStore _store;

    public InMemoryGameDefinitionRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<IReadOnlyList<GameDefinition>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<IReadOnlyList<GameDefinition>>(_store.GameDefinitions.OrderBy(game => game.DisplayName).ToArray());
    }
}
