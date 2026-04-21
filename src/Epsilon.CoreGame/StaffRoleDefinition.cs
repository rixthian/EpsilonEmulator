namespace Epsilon.CoreGame;

public sealed record StaffRoleDefinition(
    string RoleKey,
    string DisplayName,
    int Priority,
    IReadOnlyList<string> CapabilityKeys);
