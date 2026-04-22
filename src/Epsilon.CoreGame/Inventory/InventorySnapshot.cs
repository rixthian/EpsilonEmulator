namespace Epsilon.CoreGame;

public sealed record InventoryItemSnapshot(
    InventoryItemState Item,
    string PublicName,
    string InternalName,
    string InteractionTypeCode,
    bool IsStackable);

public sealed record InventorySnapshot(
    CharacterId CharacterId,
    IReadOnlyList<InventoryItemSnapshot> Items);
