using Epsilon.CoreGame;

namespace Epsilon.Content;

public sealed record EcotronRewardDefinition(
    string RewardKey,
    string DisplayName,
    ItemDefinitionId ItemDefinitionId,
    int RequiredRecycleCount,
    int RewardTier,
    bool IsVisibleInCatalog,
    IReadOnlyList<string> Tags);
