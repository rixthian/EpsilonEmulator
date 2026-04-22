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
        HashSet<string> assignedRoleKeys = _store.StaffRoleAssignments.TryGetValue(characterId, out List<StaffRoleAssignment>? assignments)
            ? assignments.Select(assignment => assignment.RoleKey).ToHashSet(StringComparer.OrdinalIgnoreCase)
            : [];

        IReadOnlyList<StaffRoleDefinition> activeRoles = _store.StaffRoleDefinitions
            .Where(role => assignedRoleKeys.Contains(role.RoleKey))
            .ToArray();

        ChatCommandScope maximumScope = activeRoles.Count == 0
            ? ChatCommandScope.Player
            : activeRoles.Max(role => role.MaxCommandScope);

        HashSet<string> effectiveCapabilities = activeRoles
            .SelectMany(role => role.CapabilityKeys)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        IReadOnlyList<ChatCommandDefinition> result = _store.ChatCommandCatalog
            .Where(command =>
                command.Scope <= maximumScope &&
                (command.RequiredCapabilityKey is null ||
                 effectiveCapabilities.Contains(command.RequiredCapabilityKey)))
            .OrderBy(command => command.Scope)
            .ThenBy(command => command.CommandKey, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return ValueTask.FromResult(result);
    }
}
