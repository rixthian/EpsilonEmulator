namespace Epsilon.CoreGame;

public sealed record CarryItemState(
    int ItemTypeId,
    string DisplayName,
    DateTime? ExpiresAtUtc);
