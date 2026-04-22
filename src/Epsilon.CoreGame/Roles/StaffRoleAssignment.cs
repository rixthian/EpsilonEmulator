namespace Epsilon.CoreGame;

public sealed record StaffRoleAssignment(
    CharacterId CharacterId,
    string RoleKey,
    bool IsPrimary,
    DateTime AssignedAtUtc);
