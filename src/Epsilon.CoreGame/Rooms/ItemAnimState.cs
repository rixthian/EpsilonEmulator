namespace Epsilon.CoreGame;

public sealed record ItemAnimState(
    long ItemId,
    string PublicName,
    string InteractionTypeCode,
    string AnimationKey,
    string StateData,
    bool IsAnimated,
    bool IsLooping);
