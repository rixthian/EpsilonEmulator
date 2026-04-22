namespace Epsilon.Content;

public interface IEffectDefinitionRepository
{
    ValueTask<IReadOnlyList<EffectDefinition>> GetVisibleAsync(
        CancellationToken cancellationToken = default);
}
