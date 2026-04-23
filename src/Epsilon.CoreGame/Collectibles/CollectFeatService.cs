using Epsilon.Content;

namespace Epsilon.CoreGame;

public sealed class CollectFeatService : ICollectFeatService
{
    private static readonly (int Level, int RequiredXp)[] LevelThresholds =
    [
        (1, 0),
        (2, 100),
        (3, 250),
        (4, 500),
        (5, 900),
        (6, 1400)
    ];

    private static readonly IReadOnlyDictionary<string, (string Name, string[] Rewards, int Emeralds)> GiftBoxes =
        new Dictionary<string, (string, string[], int)>(StringComparer.OrdinalIgnoreCase)
        {
            ["starter_crate"] = ("Starter Crate", ["collector_gift_lamp"], 50),
            ["monthly_crate"] = ("Monthly Collector Crate", ["collector_dragon_statue"], 100)
        };

    private static readonly IReadOnlyDictionary<string, (string Name, string RewardKey, int CycleHours, string RequiredCategory)> Factories =
        new Dictionary<string, (string, string, int, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["furni_factory"] = ("Furni Factory", "factory_furni_crate", 24, "rare"),
            ["clothing_factory"] = ("Clothing Factory", "factory_clothing_box", 24, "avatar")
        };

    private static readonly IReadOnlyDictionary<string, (string Name, string[] Inputs, string Output)> Recipes =
        new Dictionary<string, (string, string[], string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["collectimatic_lamp"] = ("Collecti-Matic Lamp Recipe", ["hc_gold_elephant", "rotw_dragon_lamp"], "collectimatic_prize_lamp")
        };

    private readonly ICollectorProgressRepo _progressRepository;
    private readonly ICollectibleOwnershipRepository _ownershipRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly IEmeraldLedgerService _emeraldLedgerService;
    private readonly ICollectorProfileService _collectorProfileService;
    private readonly ICharacterProfileRepository _characterProfileRepository;
    private readonly ICollectibleRepository _collectibleRepository;
    private readonly ICollectStateRepo _stateRepository;

    public CollectFeatService(
        ICollectorProgressRepo progressRepository,
        ICollectibleOwnershipRepository ownershipRepository,
        IWalletRepository walletRepository,
        IEmeraldLedgerService emeraldLedgerService,
        ICollectorProfileService collectorProfileService,
        ICharacterProfileRepository characterProfileRepository,
        ICollectibleRepository collectibleRepository,
        ICollectStateRepo stateRepository)
    {
        _progressRepository = progressRepository;
        _ownershipRepository = ownershipRepository;
        _walletRepository = walletRepository;
        _emeraldLedgerService = emeraldLedgerService;
        _collectorProfileService = collectorProfileService;
        _characterProfileRepository = characterProfileRepository;
        _collectibleRepository = collectibleRepository;
        _stateRepository = stateRepository;
    }

    public async ValueTask<CollectorProgressSnapshot> GetProgressAsync(CharacterId characterId, CancellationToken cancellationToken = default)
    {
        return await _progressRepository.GetByCharacterIdAsync(characterId, cancellationToken)
            ?? BuildProgress(characterId, 0, null);
    }

    public async ValueTask<CollectorProgressSnapshot> GrantXpAsync(CharacterId characterId, int xp, string reasonCode, CancellationToken cancellationToken = default)
    {
        if (xp <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(xp));
        }

        CollectorProgressSnapshot current = await GetProgressAsync(characterId, cancellationToken);
        CollectorProgressSnapshot updated = BuildProgress(characterId, checked(current.Xp + xp), current.LastEmeraldAccrualUtc);
        await _progressRepository.StoreAsync(updated, cancellationToken);
        return updated;
    }

    public async ValueTask<WalletSnapshot?> AccrueEmeraldsAsync(CharacterId characterId, CancellationToken cancellationToken = default)
    {
        // Atomically claim the accrual window — only one concurrent request can win.
        CollectorProgressSnapshot? claimed = await _progressRepository.TryAdvanceAccrualTimestampAsync(
            characterId, DateTime.UtcNow, requiredGapHours: 20, cancellationToken);

        if (claimed is null)
        {
            return await _walletRepository.GetByCharacterIdAsync(characterId, cancellationToken);
        }

        CollectibleOwnershipSnapshot ownership = await GetOwnershipAsync(characterId, cancellationToken);
        int ownedCount = ownership.OwnedCollectibleKeys.Count;
        int amount = Math.Max(25, ownedCount * 15 + claimed.Level * 10);
        return await _emeraldLedgerService.GrantAsync(characterId, amount, "daily_collectible_accrual", cancellationToken);
    }

    public async ValueTask<IReadOnlyList<GiftBoxSnapshot>> GetGiftBoxesAsync(CharacterId characterId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> pending = await _stateRepository.GetPendingGiftBoxesAsync(characterId, cancellationToken);

        return pending
            .Where(boxKey => GiftBoxes.ContainsKey(boxKey))
            .Select(boxKey => new GiftBoxSnapshot(
                boxKey,
                GiftBoxes[boxKey].Name,
                GiftBoxes[boxKey].Rewards,
                GiftBoxes[boxKey].Emeralds,
                true))
            .ToArray();
    }

    public async ValueTask<GiftBoxOpenResult?> OpenGiftBoxAsync(CharacterId characterId, string boxKey, CancellationToken cancellationToken = default)
    {
        if (!GiftBoxes.TryGetValue(boxKey, out var definition))
        {
            return null;
        }

        bool removed = await _stateRepository.RemovePendingGiftBoxAsync(characterId, boxKey, cancellationToken);

        if (!removed)
        {
            return null;
        }

        await AddCollectiblesAsync(characterId, definition.Rewards, cancellationToken);
        await _emeraldLedgerService.GrantAsync(characterId, definition.Emeralds, $"gift_box:{boxKey}", cancellationToken);
        await GrantXpAsync(characterId, 40, $"gift_box:{boxKey}", cancellationToken);

        return new GiftBoxOpenResult(
            boxKey,
            definition.Rewards,
            definition.Emeralds,
            await _collectorProfileService.BuildAsync(characterId, cancellationToken));
    }

    public async ValueTask<IReadOnlyList<FactorySnapshot>> GetFactoriesAsync(CharacterId characterId, CancellationToken cancellationToken = default)
    {
        CollectibleOwnershipSnapshot ownership = await GetOwnershipAsync(characterId, cancellationToken);
        List<FactorySnapshot> factories = [];
        foreach (KeyValuePair<string, (string Name, string RewardKey, int CycleHours, string RequiredCategory)> pair in Factories)
        {
            DateTime? lastClaim = await _stateRepository.GetFactoryClaimAsync(characterId, pair.Key, cancellationToken);
            DateTime? nextClaim = lastClaim?.AddHours(pair.Value.CycleHours);

            bool unlocked = ownership.OwnedCategoryKeys.Contains(pair.Value.RequiredCategory, StringComparer.OrdinalIgnoreCase);
            bool ready = unlocked && (nextClaim is null || nextClaim <= DateTime.UtcNow);
            factories.Add(new FactorySnapshot(pair.Key, pair.Value.Name, pair.Value.RewardKey, pair.Value.CycleHours, nextClaim, ready));
        }

        return factories;
    }

    public async ValueTask<GiftBoxOpenResult?> ClaimFactoryAsync(CharacterId characterId, string factoryKey, CancellationToken cancellationToken = default)
    {
        if (!Factories.TryGetValue(factoryKey, out var definition))
        {
            return null;
        }

        // Verify the character owns the required category before attempting to claim.
        CollectibleOwnershipSnapshot ownership = await GetOwnershipAsync(characterId, cancellationToken);
        if (!ownership.OwnedCategoryKeys.Contains(definition.RequiredCategory, StringComparer.OrdinalIgnoreCase))
        {
            return null;
        }

        // Atomically claim the factory slot — only one concurrent request can win.
        DateTime? claimed = await _stateRepository.TryClaimFactoryAsync(
            characterId, factoryKey, TimeSpan.FromHours(definition.CycleHours), DateTime.UtcNow, cancellationToken);

        if (claimed is null)
        {
            return null;
        }

        await AddCollectiblesAsync(characterId, [definition.RewardKey], cancellationToken);
        await _emeraldLedgerService.GrantAsync(characterId, 30, $"factory_claim:{factoryKey}", cancellationToken);
        await GrantXpAsync(characterId, 30, $"factory_claim:{factoryKey}", cancellationToken);

        return new GiftBoxOpenResult(
            factoryKey,
            [definition.RewardKey],
            30,
            await _collectorProfileService.BuildAsync(characterId, cancellationToken));
    }

    public ValueTask<IReadOnlyList<CollectRecipeSnapshot>> GetRecipesAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CollectRecipeSnapshot> recipes = Recipes.Select(pair =>
            new CollectRecipeSnapshot(pair.Key, pair.Value.Name, pair.Value.Inputs, pair.Value.Output)).ToArray();
        return ValueTask.FromResult(recipes);
    }

    public async ValueTask<GiftBoxOpenResult?> RecycleAsync(CharacterId characterId, string recipeKey, CancellationToken cancellationToken = default)
    {
        if (!Recipes.TryGetValue(recipeKey, out var recipe))
        {
            return null;
        }

        bool removed = await RemoveCollectiblesAsync(characterId, recipe.Inputs, cancellationToken);
        if (!removed)
        {
            return null;
        }

        await AddCollectiblesAsync(characterId, [recipe.Output], cancellationToken);
        await GrantXpAsync(characterId, 50, $"collectimatic:{recipeKey}", cancellationToken);

        return new GiftBoxOpenResult(
            recipeKey,
            [recipe.Output],
            0,
            await _collectorProfileService.BuildAsync(characterId, cancellationToken));
    }

    public async ValueTask<IReadOnlyList<MarketListingSnapshot>> GetMarketListingsAsync(CancellationToken cancellationToken = default)
    {
        List<MarketListingState> listings = (await _stateRepository.GetMarketListingsAsync(cancellationToken)).ToList();

        List<MarketListingSnapshot> snapshots = [];
        foreach (MarketListingState listing in listings.OrderByDescending(candidate => candidate.ListedAtUtc))
        {
            CharacterProfile? seller = await _characterProfileRepository.GetByIdAsync(listing.SellerCharacterId, cancellationToken);
            CharacterProfile? buyer = listing.BuyerCharacterId is null
                ? null
                : await _characterProfileRepository.GetByIdAsync(listing.BuyerCharacterId.Value, cancellationToken);
            snapshots.Add(new MarketListingSnapshot(
                listing.ListingId,
                listing.CollectibleKey,
                listing.PriceEmeralds,
                seller?.PublicId ?? string.Empty,
                seller?.Username ?? "unknown",
                listing.ListedAtUtc,
                listing.IsActive,
                buyer?.PublicId));
        }

        return snapshots;
    }

    public async ValueTask<MarketListingSnapshot?> CreateListingAsync(CharacterId characterId, string collectibleKey, int priceEmeralds, CancellationToken cancellationToken = default)
    {
        if (priceEmeralds <= 0)
        {
            return null;
        }

        bool removed = await RemoveCollectiblesAsync(characterId, [collectibleKey], cancellationToken);
        if (!removed)
        {
            return null;
        }

        MarketListingState listing = new(
            await _stateRepository.ReserveMarketListingIdAsync(cancellationToken),
            characterId,
            collectibleKey,
            priceEmeralds,
            DateTime.UtcNow,
            true,
            null);
        await _stateRepository.StoreMarketListingAsync(listing, cancellationToken);

        return (await GetMarketListingsAsync(cancellationToken)).FirstOrDefault(candidate => candidate.ListingId == listing.ListingId);
    }

    public async ValueTask<MarketListingSnapshot?> BuyListingAsync(CharacterId characterId, long listingId, CancellationToken cancellationToken = default)
    {
        // Atomically claim the listing — only one buyer can win this race.
        MarketListingState? listing = await _stateRepository.TryDeactivateListingAsync(listingId, cancellationToken);
        if (listing is null || listing.SellerCharacterId == characterId)
        {
            return null;
        }

        WalletSnapshot? spent = await _emeraldLedgerService.SpendAsync(characterId, listing.PriceEmeralds, $"market_buy:{listing.ListingId}", cancellationToken);
        if (spent is null)
        {
            // Buyer can't afford it — reinstate the listing so others can buy it.
            await _stateRepository.StoreMarketListingAsync(listing with { IsActive = true }, cancellationToken);
            return null;
        }

        await _stateRepository.StoreMarketListingAsync(listing with { BuyerCharacterId = characterId }, cancellationToken);
        await _emeraldLedgerService.GrantAsync(listing.SellerCharacterId, listing.PriceEmeralds, $"market_sell:{listing.ListingId}", cancellationToken);
        await AddCollectiblesAsync(characterId, [listing.CollectibleKey], cancellationToken);
        await GrantXpAsync(characterId, 20, $"market_buy:{listing.ListingId}", cancellationToken);

        return (await GetMarketListingsAsync(cancellationToken)).FirstOrDefault(candidate => candidate.ListingId == listingId);
    }

    public async ValueTask<CollectiblesPublicSnapshot> GetPublicSnapshotAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CollectibleDefinition> visible = await _collectibleRepository.GetVisibleAsync(cancellationToken);
        IReadOnlyList<MarketListingSnapshot> listings = await GetMarketListingsAsync(cancellationToken);
        int totalCollectors = await CountCollectorsAsync(cancellationToken);

        return new CollectiblesPublicSnapshot(
            visible,
            listings.Where(candidate => candidate.IsActive).ToArray(),
            totalCollectors,
            visible.Count);
    }

    private async ValueTask<CollectibleOwnershipSnapshot> GetOwnershipAsync(CharacterId characterId, CancellationToken cancellationToken)
    {
        return await _ownershipRepository.GetByCharacterIdAsync(characterId, cancellationToken)
            ?? new CollectibleOwnershipSnapshot(characterId, null, [], [], "runtime_default", DateTime.UtcNow);
    }

    private async ValueTask<IReadOnlyDictionary<string, string>> BuildCategoryLookupAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<CollectibleDefinition> visible = await _collectibleRepository.GetVisibleAsync(cancellationToken);
        return visible.ToDictionary(
            definition => definition.CollectibleKey,
            definition => definition.CategoryKey,
            StringComparer.OrdinalIgnoreCase);
    }

    private async ValueTask AddCollectiblesAsync(CharacterId characterId, IReadOnlyList<string> keys, CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<string, string> categoryLookup = await BuildCategoryLookupAsync(cancellationToken);
        await _ownershipRepository.AddKeysAsync(characterId, keys, categoryLookup, cancellationToken);
    }

    private async ValueTask<bool> RemoveCollectiblesAsync(CharacterId characterId, IReadOnlyList<string> keys, CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<string, string> categoryLookup = await BuildCategoryLookupAsync(cancellationToken);
        (_, bool success) = await _ownershipRepository.TryRemoveKeysAsync(characterId, keys, categoryLookup, cancellationToken);
        return success;
    }

    private static CollectorProgressSnapshot BuildProgress(CharacterId characterId, int xp, DateTime? lastEmeraldAccrualUtc)
    {
        int level = LevelThresholds.Last(threshold => xp >= threshold.RequiredXp).Level;
        int currentLevelXp = LevelThresholds.Last(threshold => threshold.Level == level).RequiredXp;
        int? nextLevelXp = LevelThresholds.FirstOrDefault(threshold => threshold.Level == level + 1).RequiredXp;
        if (nextLevelXp == 0 && level == LevelThresholds[^1].Level)
        {
            nextLevelXp = null;
        }

        return new CollectorProgressSnapshot(
            characterId,
            xp,
            level,
            currentLevelXp,
            nextLevelXp,
            ResolveMonthlyRewardTier(level),
            lastEmeraldAccrualUtc);
    }

    private static string ResolveMonthlyRewardTier(int level)
    {
        if (level >= 6)
        {
            return "diamond";
        }

        if (level >= 4)
        {
            return "gold";
        }

        if (level >= 2)
        {
            return "silver";
        }

        return "bronze";
    }

    private ValueTask<int> CountCollectorsAsync(CancellationToken cancellationToken)
    {
        return _ownershipRepository.CountWithOwnedCollectiblesAsync(cancellationToken);
    }
}
