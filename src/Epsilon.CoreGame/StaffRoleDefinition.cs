namespace Epsilon.CoreGame;

public sealed record StaffRoleDefinition(
    string RoleKey,
    string DisplayName,
    int RankLevel,
    ChatCommandScope MaxCommandScope,
    IReadOnlyList<string> CapabilityKeys);
