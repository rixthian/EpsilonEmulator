namespace Epsilon.CoreGame;

public interface IVoucherRedemptionRepository
{
    ValueTask<bool> TryRedeemAsync(
        CharacterId characterId,
        string voucherCode,
        CancellationToken cancellationToken = default);
}
