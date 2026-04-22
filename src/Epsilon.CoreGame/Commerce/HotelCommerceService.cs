using Epsilon.Content;

namespace Epsilon.CoreGame;

public sealed class HotelCommerceService : IHotelCommerceService
{
    private readonly ICharacterProfileRepository _characterProfiles;
    private readonly IWalletRepository _walletRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ICatalogPageRepository _catalogPageRepository;
    private readonly ICatalogOfferRepository _catalogOfferRepository;
    private readonly IItemDefinitionRepository _itemDefinitionRepository;
    private readonly IVoucherRepository _voucherRepository;
    private readonly IVoucherRedemptionRepository _voucherRedemptionRepository;

    public HotelCommerceService(
        ICharacterProfileRepository characterProfiles,
        IWalletRepository walletRepository,
        IInventoryRepository inventoryRepository,
        ICatalogPageRepository catalogPageRepository,
        ICatalogOfferRepository catalogOfferRepository,
        IItemDefinitionRepository itemDefinitionRepository,
        IVoucherRepository voucherRepository,
        IVoucherRedemptionRepository voucherRedemptionRepository)
    {
        _characterProfiles = characterProfiles;
        _walletRepository = walletRepository;
        _inventoryRepository = inventoryRepository;
        _catalogPageRepository = catalogPageRepository;
        _catalogOfferRepository = catalogOfferRepository;
        _itemDefinitionRepository = itemDefinitionRepository;
        _voucherRepository = voucherRepository;
        _voucherRedemptionRepository = voucherRedemptionRepository;
    }

    public async ValueTask<IReadOnlyList<CatalogPageSnapshot>> GetCatalogPagesAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        CharacterProfile? character = await _characterProfiles.GetByIdAsync(characterId, cancellationToken);
        if (character is null)
        {
            return [];
        }

        IReadOnlyList<CatalogPageDefinition> pages = await _catalogPageRepository.GetVisibleByCharacterIdAsync(
            characterId,
            cancellationToken);

        List<CatalogPageSnapshot> snapshots = new(pages.Count);
        foreach (CatalogPageDefinition page in pages.OrderBy(candidate => candidate.OrderNumber))
        {
            CatalogPageSnapshot? snapshot = await BuildCatalogPageSnapshotAsync(character, page, cancellationToken);
            if (snapshot is not null)
            {
                snapshots.Add(snapshot);
            }
        }

        return snapshots;
    }

    public async ValueTask<CatalogPageSnapshot?> GetCatalogPageAsync(
        CharacterId characterId,
        CatalogPageId catalogPageId,
        CancellationToken cancellationToken = default)
    {
        CharacterProfile? character = await _characterProfiles.GetByIdAsync(characterId, cancellationToken);
        if (character is null)
        {
            return null;
        }

        CatalogPageDefinition? page = await _catalogPageRepository.GetByIdAsync(catalogPageId, cancellationToken);
        if (page is null || !IsPageVisibleToCharacter(page, character))
        {
            return null;
        }

        return await BuildCatalogPageSnapshotAsync(character, page, cancellationToken);
    }

    public async ValueTask<InventorySnapshot> GetInventoryAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<InventoryItemState> items = await _inventoryRepository.GetByCharacterIdAsync(characterId, cancellationToken);
        return await BuildInventorySnapshotAsync(characterId, items, cancellationToken);
    }

    public async ValueTask<CatalogPurchaseResult> PurchaseAsync(
        CatalogPurchaseRequest request,
        CancellationToken cancellationToken = default)
    {
        CharacterProfile? character = await _characterProfiles.GetByIdAsync(request.CharacterId, cancellationToken);
        if (character is null)
        {
            return new CatalogPurchaseResult(false, "Character could not be resolved.", null, null, null);
        }

        CatalogOfferDefinition? offer = await _catalogOfferRepository.GetByIdAsync(request.CatalogOfferId, cancellationToken);
        if (offer is null)
        {
            return new CatalogPurchaseResult(false, "Catalog offer could not be resolved.", null, null, null);
        }

        if (!TryValidateOffer(offer, out string? offerFailure))
        {
            return new CatalogPurchaseResult(
                false,
                offerFailure ?? "Catalog offer is invalid.",
                request.CatalogOfferId,
                null,
                null);
        }

        CatalogPageDefinition? page = await _catalogPageRepository.GetByIdAsync(offer.CatalogPageId, cancellationToken);
        if (page is null || !IsPageVisibleToCharacter(page, character))
        {
            return new CatalogPurchaseResult(false, "Catalog page is not available for this character.", request.CatalogOfferId, null, null);
        }

        WalletSnapshot wallet = await _walletRepository.GetByCharacterIdAsync(request.CharacterId, cancellationToken)
            ?? new WalletSnapshot(request.CharacterId, [], []);

        if (!HasSufficientFunds(wallet, "credits", offer.CreditsCost))
        {
            return new CatalogPurchaseResult(false, "Insufficient credits for this offer.", request.CatalogOfferId, null, wallet);
        }

        if (!HasSufficientFunds(wallet, "duckets", offer.ActivityPointsCost))
        {
            return new CatalogPurchaseResult(false, "Insufficient duckets for this offer.", request.CatalogOfferId, null, wallet);
        }

        if (!HasSufficientFunds(wallet, "snow", offer.SnowCost))
        {
            return new CatalogPurchaseResult(false, "Insufficient snow for this offer.", request.CatalogOfferId, null, wallet);
        }

        WalletSnapshot updatedWallet = ApplyPurchase(wallet, offer);
        await _walletRepository.StoreAsync(updatedWallet, cancellationToken);

        List<ItemDefinitionId> purchasedItems = [];
        foreach (CatalogOfferProductDefinition product in offer.Products)
        {
            for (int iteration = 0; iteration < Math.Max(product.Amount, 1); iteration++)
            {
                purchasedItems.Add(product.ItemDefinitionId);
            }
        }

        await _inventoryRepository.AddItemsAsync(
            request.CharacterId,
            purchasedItems,
            $"catalog_purchase:{offer.CatalogOfferId.Value}",
            cancellationToken);

        InventorySnapshot inventory = await GetInventoryAsync(request.CharacterId, cancellationToken);

        return new CatalogPurchaseResult(
            true,
            $"Purchased '{offer.CatalogName}' successfully.",
            request.CatalogOfferId,
            inventory,
            updatedWallet);
    }

    public async ValueTask<RedeemVoucherResult> RedeemVoucherAsync(
        RedeemVoucherRequest request,
        CancellationToken cancellationToken = default)
    {
        CharacterProfile? character = await _characterProfiles.GetByIdAsync(request.CharacterId, cancellationToken);
        if (character is null)
        {
            return new RedeemVoucherResult(false, "Character could not be resolved.", null);
        }

        if (string.IsNullOrWhiteSpace(request.VoucherCode))
        {
            return new RedeemVoucherResult(false, "Voucher code is required.", null);
        }

        VoucherDefinition? voucher = await _voucherRepository.GetByCodeAsync(
            request.VoucherCode.Trim(),
            cancellationToken);
        if (voucher is null || !voucher.IsEnabled)
        {
            return new RedeemVoucherResult(false, "Voucher code is not valid.", null);
        }

        if (!TryValidateVoucher(voucher, out string? voucherFailure))
        {
            return new RedeemVoucherResult(false, voucherFailure ?? "Voucher definition is invalid.", null);
        }

        bool redeemed = await _voucherRedemptionRepository.TryRedeemAsync(
            request.CharacterId,
            voucher.VoucherCode,
            cancellationToken);
        if (!redeemed)
        {
            return new RedeemVoucherResult(false, "Voucher code has already been redeemed.", null);
        }

        WalletSnapshot wallet = await _walletRepository.GetByCharacterIdAsync(request.CharacterId, cancellationToken)
            ?? new WalletSnapshot(request.CharacterId, [], []);
        WalletSnapshot updatedWallet = ApplyCredit(
            wallet,
            voucher.RewardCurrencyCode,
            voucher.RewardAmount,
            $"voucher:{voucher.VoucherCode}");
        await _walletRepository.StoreAsync(updatedWallet, cancellationToken);

        return new RedeemVoucherResult(true, $"Redeemed '{voucher.DisplayName}'.", updatedWallet);
    }

    private static bool TryValidateOffer(
        CatalogOfferDefinition offer,
        out string? failure)
    {
        if (offer.Products.Count == 0)
        {
            failure = "Catalog offer does not define any products.";
            return false;
        }

        if (offer.CreditsCost < 0 || offer.ActivityPointsCost < 0 || offer.SnowCost < 0)
        {
            failure = "Catalog offer contains a negative currency cost.";
            return false;
        }

        if (offer.Products.Any(product => product.Amount <= 0))
        {
            failure = "Catalog offer contains a product with an invalid quantity.";
            return false;
        }

        failure = null;
        return true;
    }

    private static bool TryValidateVoucher(
        VoucherDefinition voucher,
        out string? failure)
    {
        if (string.IsNullOrWhiteSpace(voucher.RewardCurrencyCode))
        {
            failure = "Voucher reward currency is missing.";
            return false;
        }

        if (voucher.RewardAmount <= 0)
        {
            failure = "Voucher reward amount must be greater than zero.";
            return false;
        }

        failure = null;
        return true;
    }

    private async ValueTask<CatalogPageSnapshot?> BuildCatalogPageSnapshotAsync(
        CharacterProfile character,
        CatalogPageDefinition page,
        CancellationToken cancellationToken)
    {
        if (!IsPageVisibleToCharacter(page, character))
        {
            return null;
        }

        IReadOnlyList<CatalogOfferDefinition> offers = await _catalogOfferRepository.GetByPageIdAsync(
            page.CatalogPageId,
            cancellationToken);

        List<CatalogOfferSnapshot> offerSnapshots = new(offers.Count);
        foreach (CatalogOfferDefinition offer in offers)
        {
            List<string> productNames = [];
            int totalProductCount = 0;
            foreach (CatalogOfferProductDefinition product in offer.Products)
            {
                totalProductCount += Math.Max(product.Amount, 1);
                ItemDefinition? definition = await _itemDefinitionRepository.GetByIdAsync(product.ItemDefinitionId, cancellationToken);
                if (definition is not null)
                {
                    productNames.Add(product.Amount > 1
                        ? $"{definition.PublicName} x{product.Amount}"
                        : definition.PublicName);
                }
            }

            offerSnapshots.Add(new CatalogOfferSnapshot(offer, productNames, totalProductCount));
        }

        return new CatalogPageSnapshot(page, offerSnapshots);
    }

    private async ValueTask<InventorySnapshot> BuildInventorySnapshotAsync(
        CharacterId characterId,
        IReadOnlyList<InventoryItemState> items,
        CancellationToken cancellationToken)
    {
        List<InventoryItemSnapshot> snapshots = new(items.Count);
        foreach (InventoryItemState item in items.OrderByDescending(candidate => candidate.CreatedAtUtc))
        {
            ItemDefinition? definition = await _itemDefinitionRepository.GetByIdAsync(item.ItemDefinitionId, cancellationToken);
            if (definition is null)
            {
                continue;
            }

            snapshots.Add(new InventoryItemSnapshot(
                item,
                definition.PublicName,
                definition.InternalName,
                definition.InteractionTypeCode,
                definition.AllowInventoryStack));
        }

        return new InventorySnapshot(characterId, snapshots);
    }

    private static bool IsPageVisibleToCharacter(CatalogPageDefinition page, CharacterProfile character)
    {
        if (!page.IsVisible || !page.IsEnabled)
        {
            return false;
        }

        return character.AccountId.Value >= 0 && character.CreditsBalance >= 0 && character.RespectPoints >= 0 &&
               page.MinimumRank <= 0;
    }

    private static bool HasSufficientFunds(
        WalletSnapshot wallet,
        string currencyCode,
        int requiredAmount)
    {
        if (requiredAmount <= 0)
        {
            return true;
        }

        WalletBalance? balance = wallet.Balances.FirstOrDefault(candidate =>
            string.Equals(candidate.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase));

        return balance is not null && balance.Amount >= requiredAmount;
    }

    private static WalletSnapshot ApplyPurchase(WalletSnapshot wallet, CatalogOfferDefinition offer)
    {
        List<WalletBalance> balances = wallet.Balances.ToList();
        List<WalletLedgerEntry> ledger = wallet.RecentEntries.ToList();

        ApplyDebit(balances, ledger, "credits", offer.CreditsCost, $"catalog:{offer.CatalogOfferId.Value}");
        ApplyDebit(balances, ledger, "duckets", offer.ActivityPointsCost, $"catalog:{offer.CatalogOfferId.Value}");
        ApplyDebit(balances, ledger, "snow", offer.SnowCost, $"catalog:{offer.CatalogOfferId.Value}");

        return new WalletSnapshot(wallet.CharacterId, balances, ledger
            .OrderByDescending(entry => entry.CreatedAtUtc)
            .Take(20)
            .ToArray());
    }

    private static WalletSnapshot ApplyCredit(
        WalletSnapshot wallet,
        string currencyCode,
        int amount,
        string reasonCode)
    {
        List<WalletBalance> balances = wallet.Balances.ToList();
        List<WalletLedgerEntry> ledger = wallet.RecentEntries.ToList();

        int index = balances.FindIndex(candidate =>
            string.Equals(candidate.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            WalletBalance current = balances[index];
            balances[index] = current with { Amount = current.Amount + amount };
        }
        else
        {
            balances.Add(new WalletBalance(currencyCode, amount));
        }

        ledger.Insert(0, new WalletLedgerEntry(currencyCode, amount, reasonCode, DateTime.UtcNow));

        return new WalletSnapshot(wallet.CharacterId, balances, ledger
            .OrderByDescending(entry => entry.CreatedAtUtc)
            .Take(20)
            .ToArray());
    }

    private static void ApplyDebit(
        List<WalletBalance> balances,
        List<WalletLedgerEntry> ledger,
        string currencyCode,
        int amount,
        string reasonCode)
    {
        if (amount <= 0)
        {
            return;
        }

        int index = balances.FindIndex(candidate =>
            string.Equals(candidate.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            return;
        }

        WalletBalance current = balances[index];
        balances[index] = current with
        {
            Amount = current.Amount - amount
        };

        ledger.Insert(0, new WalletLedgerEntry(currencyCode, -amount, reasonCode, DateTime.UtcNow));
    }
}
