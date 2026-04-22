using Epsilon.CoreGame;

namespace Epsilon.Content;

public interface IItemDefinitionRepository
{
    ValueTask<ItemDefinition?> GetByIdAsync(ItemDefinitionId itemDefinitionId, CancellationToken cancellationToken = default);
}

