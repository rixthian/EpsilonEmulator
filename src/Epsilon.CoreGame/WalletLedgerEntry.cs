namespace Epsilon.CoreGame;

public sealed record WalletLedgerEntry(
    string CurrencyCode,
    int Delta,
    string ReasonCode,
    DateTime CreatedAtUtc);
