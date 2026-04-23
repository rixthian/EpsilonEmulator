namespace Epsilon.CoreGame;

public interface IWalletRepository
{
    ValueTask<WalletSnapshot?> GetByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);

    ValueTask StoreAsync(
        WalletSnapshot wallet,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically checks that all debits can be satisfied and applies them.
    /// Returns the updated wallet on success, or null if any balance was insufficient.
    /// </summary>
    ValueTask<WalletSnapshot?> TryApplyDebitsAsync(
        CharacterId characterId,
        IReadOnlyList<(string CurrencyCode, int Amount, string ReasonCode)> debits,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically credits a single currency balance. Always succeeds.
    /// </summary>
    ValueTask<WalletSnapshot> ApplyCreditAsync(
        CharacterId characterId,
        string currencyCode,
        int amount,
        string reasonCode,
        CancellationToken cancellationToken = default);
}
