namespace Epsilon.CoreGame;

public sealed record ActorAnimState(
    long ActorId,
    RoomActorKind ActorKind,
    string DisplayName,
    string AnimationKey,
    string? VariantKey,
    IReadOnlyList<string> ActiveLayerKeys,
    string? CarryItemName);
