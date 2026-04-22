namespace Epsilon.CoreGame;

public sealed record InventoryItemState(
    ItemId ItemId,
    CharacterId OwnerCharacterId,
    ItemDefinitionId ItemDefinitionId,
    string StateData,
    DateTime CreatedAtUtc);
