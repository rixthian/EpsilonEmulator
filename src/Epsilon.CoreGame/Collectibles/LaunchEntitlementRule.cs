namespace Epsilon.CoreGame;

public sealed record LaunchEntitlementRule(
    string RuleKey,
    string DisplayName,
    string RequirementKind,
    string RequiredValue,
    bool IsRequired,
    bool IsSatisfied);
