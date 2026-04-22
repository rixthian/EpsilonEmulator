namespace Epsilon.Games;

public interface IGameDefinitionRepository
{
    ValueTask<IReadOnlyList<GameDefinition>> GetAllAsync(
        CancellationToken cancellationToken = default);
}
