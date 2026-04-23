namespace Epsilon.CoreGame;

public sealed class EmeraldLedgerService : IEmeraldLedgerService
{
    private const string CurrencyCode = "emeralds";

    private readonly IWalletRepository _walletRepository;
    private readonly IHotelEventBus _hotelEventBus;

    public EmeraldLedgerService(
        IWalletRepository walletRepository,
        IHotelEventBus hotelEventBus)
    {
        _walletRepository = walletRepository;
        _hotelEventBus = hotelEventBus;
    }

    public async ValueTask<WalletSnapshot?> GrantAsync(
        CharacterId characterId,
        int amount,
        string reasonCode,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Emerald grant amount must be positive.");
        }

        WalletSnapshot updatedWallet = await _walletRepository.ApplyCreditAsync(
            characterId, CurrencyCode, amount, reasonCode, cancellationToken);

        await _hotelEventBus.PublishAsync(
            HotelEventKind.WalletAdjusted,
            new WalletAdjustedEvent(characterId, characterId, CurrencyCode, amount, reasonCode),
            characterId,
            null,
            cancellationToken);

        return updatedWallet;
    }

    public async ValueTask<WalletSnapshot?> SpendAsync(
        CharacterId characterId,
        int amount,
        string reasonCode,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Emerald spend amount must be positive.");
        }

        WalletSnapshot? updatedWallet = await _walletRepository.TryApplyDebitsAsync(
            characterId,
            [(CurrencyCode, amount, reasonCode)],
            cancellationToken);

        if (updatedWallet is null)
        {
            return null;
        }

        await _hotelEventBus.PublishAsync(
            HotelEventKind.WalletAdjusted,
            new WalletAdjustedEvent(characterId, characterId, CurrencyCode, -amount, reasonCode),
            characterId,
            null,
            cancellationToken);

        return updatedWallet;
    }
}
