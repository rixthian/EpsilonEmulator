using Epsilon.CoreGame;

namespace Epsilon.Content;

public sealed record ItemDefinition(
    ItemDefinitionId ItemDefinitionId,
    string PublicName,
    string InternalName,
    string ItemTypeCode,
    int SpriteId,
    double StackHeight,
    bool CanStack,
    bool CanSit,
    bool IsWalkable,
    bool AllowRecycle,
    bool AllowTrade,
    bool AllowMarketplaceSell,
    bool AllowGift,
    bool AllowInventoryStack,
    string InteractionTypeCode,
    int InteractionModesCount);

