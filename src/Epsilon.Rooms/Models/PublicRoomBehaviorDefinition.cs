namespace Epsilon.Rooms;

public sealed record PublicRoomBehaviorDefinition(
    string BehaviorKey,
    string AssetPackageKey,
    string DisplayName,
    string InteractionType,
    bool IsAnimated,
    bool IsEnabled,
    IReadOnlyList<string> Tags);
