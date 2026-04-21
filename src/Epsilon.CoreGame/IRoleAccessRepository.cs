namespace Epsilon.CoreGame;

public interface IRoleAccessRepository
{
    ValueTask<IReadOnlyList<StaffRoleDefinition>> GetRoleDefinitionsAsync(
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<AccessCapability>> GetCapabilityCatalogAsync(
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<StaffRoleAssignment>> GetAssignmentsByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);
}
