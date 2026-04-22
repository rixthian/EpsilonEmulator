namespace Epsilon.Content;

public sealed record EffectDefinition(
    string EffectKey,
    string PublicName,
    string Description,
    int SpriteId,
    int CreditsCost,
    int ActivityPointsCost,
    bool ClubOnly,
    int DurationSeconds,
    string PreviewAnimationKey);
