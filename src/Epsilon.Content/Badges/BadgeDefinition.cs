namespace Epsilon.Content;

public sealed record BadgeDefinition(
    string BadgeCode,
    string BadgeName,
    string? BadgeGroup,
    string? RequiredRight,
    string AssetPath,
    string AssetKind);
