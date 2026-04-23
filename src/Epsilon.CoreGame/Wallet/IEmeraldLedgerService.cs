namespace Epsilon.CoreGame;

public interface IEmeraldLedgerService
{
    ValueTask<WalletSnapshot?> GrantAsync(
        CharacterId characterId,
        int amount,
        string reasonCode,
        CancellationToken cancellationToken = default);

    ValueTask<WalletSnapshot?> SpendAsync(
        CharacterId characterId,
        int amount,
        string reasonCode,
        CancellationToken cancellationToken = default);
}
