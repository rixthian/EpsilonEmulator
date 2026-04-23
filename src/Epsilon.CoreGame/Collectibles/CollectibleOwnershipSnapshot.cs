namespace Epsilon.CoreGame;

public sealed record CollectibleOwnershipSnapshot(
    CharacterId CharacterId,
    string? WalletAddress,
    IReadOnlyList<string> OwnedCollectibleKeys,
    IReadOnlyList<string> OwnedCategoryKeys,
    string SourceKind,
    DateTime ObservedAtUtc);
