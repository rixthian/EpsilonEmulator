namespace Epsilon.CoreGame;

public sealed record LaunchEntitlementSnapshot(
    bool CanLaunch,
    IReadOnlyList<LaunchEntitlementRule> Rules,
    IReadOnlyList<string> MissingRequirementKeys,
    DateTime EvaluatedAtUtc);
