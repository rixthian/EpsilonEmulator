namespace Epsilon.CoreGame;

public sealed class LaunchEntitlementService : ILaunchEntitlementService
{
    private readonly IWalletLinkRepository _walletLinkRepository;
    private readonly ICollectibleOwnershipRepository _collectibleOwnershipRepository;

    public LaunchEntitlementService(
        IWalletLinkRepository walletLinkRepository,
        ICollectibleOwnershipRepository collectibleOwnershipRepository)
    {
        _walletLinkRepository = walletLinkRepository;
        _collectibleOwnershipRepository = collectibleOwnershipRepository;
    }

    public async ValueTask<LaunchEntitlementSnapshot> EvaluateAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<WalletLinkSnapshot> walletLinks =
            await _walletLinkRepository.GetByCharacterIdAsync(characterId, cancellationToken);
        CollectibleOwnershipSnapshot? ownership =
            await _collectibleOwnershipRepository.GetByCharacterIdAsync(characterId, cancellationToken);

        bool hasWalletLink = walletLinks.Count > 0;
        bool hasPremiumCollectible = ownership is not null &&
            (ownership.OwnedCollectibleKeys.Count > 0 || ownership.OwnedCategoryKeys.Count > 0);

        LaunchEntitlementRule[] rules =
        [
            new LaunchEntitlementRule(
                "wallet_link",
                "Wallet link",
                "wallet_link",
                "linked_wallet",
                true,
                hasWalletLink),
            new LaunchEntitlementRule(
                "premium_collectible",
                "Premium collectible access",
                "minimum_owned_collectibles",
                "1",
                true,
                hasPremiumCollectible)
        ];

        IReadOnlyList<string> missingRequirements = rules
            .Where(rule => rule.IsRequired && !rule.IsSatisfied)
            .Select(rule => rule.RuleKey)
            .ToArray();

        return new LaunchEntitlementSnapshot(
            missingRequirements.Count == 0,
            rules,
            missingRequirements,
            DateTime.UtcNow);
    }
}
