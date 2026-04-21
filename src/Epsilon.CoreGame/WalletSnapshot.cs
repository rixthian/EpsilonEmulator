namespace Epsilon.CoreGame;

public sealed record WalletSnapshot(
    CharacterId CharacterId,
    IReadOnlyList<WalletBalance> Balances,
    IReadOnlyList<WalletLedgerEntry> RecentEntries);
