namespace Epsilon.CoreGame;

public sealed record AchievementProgress(
    string AchievementKey,
    int Level,
    int Progress,
    int Target,
    DateTime? LastUpdatedAtUtc);
