using Epsilon.CoreGame;

namespace Epsilon.Persistence;

internal sealed class InMemoryRoleAccessRepository : IRoleAccessRepository
{
    private readonly InMemoryHotelStore _store;

    public InMemoryRoleAccessRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<IReadOnlyList<StaffRoleDefinition>> GetRoleDefinitionsAsync(
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<IReadOnlyList<StaffRoleDefinition>>(_store.StaffRoleDefinitions);
    }

    public ValueTask<IReadOnlyList<AccessCapability>> GetCapabilityCatalogAsync(
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<IReadOnlyList<AccessCapability>>(_store.AccessCapabilities);
    }

    public ValueTask<IReadOnlyList<StaffRoleAssignment>> GetAssignmentsByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<StaffRoleAssignment> result =
            _store.StaffRoleAssignments.TryGetValue(characterId, out List<StaffRoleAssignment>? assignments)
                ? assignments
                : [];

        return ValueTask.FromResult(result);
    }
}
