using Epsilon.Content;

namespace Epsilon.CoreGame;

public sealed record CollectorProgressSnapshot(
    CharacterId CharacterId,
    int Xp,
    int Level,
    int CurrentLevelXp,
    int? NextLevelXp,
    string MonthlyRewardTier,
    DateTime? LastEmeraldAccrualUtc);

public sealed record GiftBoxSnapshot(
    string BoxKey,
    string DisplayName,
    IReadOnlyList<string> RewardCollectibleKeys,
    int EmeraldReward,
    bool IsAvailable);

public sealed record GiftBoxOpenResult(
    string BoxKey,
    IReadOnlyList<string> GrantedCollectibleKeys,
    int GrantedEmeralds,
    CollectorProfileSnapshot Collector);

public sealed record FactorySnapshot(
    string FactoryKey,
    string DisplayName,
    string RewardCollectibleKey,
    int CycleHours,
    DateTime? NextClaimAtUtc,
    bool IsReady);

public sealed record CollectRecipeSnapshot(
    string RecipeKey,
    string DisplayName,
    IReadOnlyList<string> InputCollectibleKeys,
    string OutputCollectibleKey);

public sealed record MarketListingSnapshot(
    long ListingId,
    string CollectibleKey,
    int PriceEmeralds,
    string SellerPublicId,
    string SellerName,
    DateTime ListedAtUtc,
    bool IsActive,
    string? BuyerPublicId);

public sealed record CollectiblesPublicSnapshot(
    IReadOnlyList<CollectibleDefinition> VisibleCollectibles,
    IReadOnlyList<MarketListingSnapshot> ActiveListings,
    int TotalCollectors,
    int TotalVisibleCollectibles);

public sealed record MarketListingState(
    long ListingId,
    CharacterId SellerCharacterId,
    string CollectibleKey,
    int PriceEmeralds,
    DateTime ListedAtUtc,
    bool IsActive,
    CharacterId? BuyerCharacterId);
