using Epsilon.CoreGame;

namespace Epsilon.Persistence;

internal sealed class InMemoryChatCommandRepository : IChatCommandRepository
{
    private readonly InMemoryHotelStore _store;

    public InMemoryChatCommandRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<IReadOnlyList<ChatCommandDefinition>> GetAvailableByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ChatCommandDefinition> result = _store.ChatCommands.TryGetValue(characterId, out List<ChatCommandDefinition>? commands)
            ? commands
            : [];

        return ValueTask.FromResult(result);
    }
}
