namespace Epsilon.CoreGame;

public sealed class AccessControlService : IAccessControlService
{
    private readonly IRoleAccessRepository _roleAccessRepository;

    public AccessControlService(IRoleAccessRepository roleAccessRepository)
    {
        _roleAccessRepository = roleAccessRepository;
    }

    public async ValueTask<bool> HasCapabilityAsync(
        CharacterId characterId,
        string capabilityKey,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<StaffRoleAssignment> assignments =
            await _roleAccessRepository.GetAssignmentsByCharacterIdAsync(characterId, cancellationToken);
        if (assignments.Count == 0)
        {
            return false;
        }

        HashSet<string> assignedRoles = assignments
            .Select(assignment => assignment.RoleKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        IReadOnlyList<StaffRoleDefinition> roleDefinitions =
            await _roleAccessRepository.GetRoleDefinitionsAsync(cancellationToken);

        return roleDefinitions.Any(role =>
            assignedRoles.Contains(role.RoleKey) &&
            role.CapabilityKeys.Any(candidate => string.Equals(candidate, capabilityKey, StringComparison.OrdinalIgnoreCase)));
    }
}
