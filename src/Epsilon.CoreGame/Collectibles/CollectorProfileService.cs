namespace Epsilon.CoreGame;

public sealed class CollectorProfileService : ICollectorProfileService
{
    private readonly IWalletLinkRepository _walletLinkRepository;
    private readonly ICollectibleOwnershipRepository _collectibleOwnershipRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly ICollectorProgressRepo _collectorProgressRepository;

    public CollectorProfileService(
        IWalletLinkRepository walletLinkRepository,
        ICollectibleOwnershipRepository collectibleOwnershipRepository,
        IWalletRepository walletRepository,
        ICollectorProgressRepo collectorProgressRepository)
    {
        _walletLinkRepository = walletLinkRepository;
        _collectibleOwnershipRepository = collectibleOwnershipRepository;
        _walletRepository = walletRepository;
        _collectorProgressRepository = collectorProgressRepository;
    }

    public async ValueTask<CollectorProfileSnapshot> BuildAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<WalletLinkSnapshot> walletLinks =
            await _walletLinkRepository.GetByCharacterIdAsync(characterId, cancellationToken);
        CollectibleOwnershipSnapshot? ownership =
            await _collectibleOwnershipRepository.GetByCharacterIdAsync(characterId, cancellationToken);
        WalletSnapshot? wallet = await _walletRepository.GetByCharacterIdAsync(characterId, cancellationToken);
        CollectorProgressSnapshot progress =
            await _collectorProgressRepository.GetByCharacterIdAsync(characterId, cancellationToken)
            ?? new CollectorProgressSnapshot(characterId, 0, 1, 0, 100, "bronze", null);

        int emeraldBalance = wallet?.Balances
            .FirstOrDefault(balance => string.Equals(balance.CurrencyCode, "emeralds", StringComparison.OrdinalIgnoreCase))
            ?.Amount ?? 0;
        int ownedCollectibleCount = ownership?.OwnedCollectibleKeys.Count ?? 0;

        return new CollectorProfileSnapshot(
            characterId,
            walletLinks,
            ownership,
            progress,
            emeraldBalance,
            ownedCollectibleCount,
            ResolveCollectorTier(ownedCollectibleCount, emeraldBalance, progress.Level));
    }

    private static string ResolveCollectorTier(int ownedCollectibleCount, int emeraldBalance, int level)
    {
        if (level >= 6 || ownedCollectibleCount >= 5 || emeraldBalance >= 2500)
        {
            return "legend";
        }

        if (level >= 4 || ownedCollectibleCount >= 3 || emeraldBalance >= 1000)
        {
            return "curator";
        }

        if (ownedCollectibleCount >= 1 || emeraldBalance > 0)
        {
            return "collector";
        }

        return "visitor";
    }
}
