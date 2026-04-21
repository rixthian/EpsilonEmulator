namespace Epsilon.CoreGame;

public interface IChatCommandRepository
{
    ValueTask<IReadOnlyList<ChatCommandDefinition>> GetAvailableByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);
}
