namespace Epsilon.CoreGame;

public sealed record CollectorProfileSnapshot(
    CharacterId CharacterId,
    IReadOnlyList<WalletLinkSnapshot> WalletLinks,
    CollectibleOwnershipSnapshot? Ownership,
    CollectorProgressSnapshot Progress,
    int EmeraldBalance,
    int OwnedCollectibleCount,
    string CollectorTier);
