namespace Epsilon.CoreGame;

public sealed record BadgeAssignment(
    string BadgeCode,
    int SlotIndex,
    bool IsPinned);
