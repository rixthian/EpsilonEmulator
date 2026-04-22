namespace Epsilon.CoreGame;

public sealed record SubscriptionRecord(
    CharacterId CharacterId,
    SubscriptionType SubscriptionType,
    DateTime ActivatedAtUtc,
    DateTime ExpiresAtUtc);

