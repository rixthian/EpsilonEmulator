using Epsilon.CoreGame;

namespace Epsilon.Persistence;

internal sealed class InMemoryWalletRepository : IWalletRepository
{
    private readonly InMemoryHotelStore _store;
    private readonly object _syncRoot = new();

    public InMemoryWalletRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<WalletSnapshot?> GetByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            _store.Wallets.TryGetValue(characterId, out WalletSnapshot? wallet);
            return ValueTask.FromResult(wallet);
        }
    }

    public ValueTask StoreAsync(
        WalletSnapshot wallet,
        CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            _store.Wallets[wallet.CharacterId] = wallet;
            return ValueTask.CompletedTask;
        }
    }

    public ValueTask<WalletSnapshot?> TryApplyDebitsAsync(
        CharacterId characterId,
        IReadOnlyList<(string CurrencyCode, int Amount, string ReasonCode)> debits,
        CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            _store.Wallets.TryGetValue(characterId, out WalletSnapshot? current);
            current ??= new WalletSnapshot(characterId, [], []);

            // Verify all balances are sufficient before modifying anything.
            foreach ((string currencyCode, int amount, _) in debits)
            {
                if (amount <= 0)
                {
                    continue;
                }

                WalletBalance? balance = current.Balances.FirstOrDefault(b =>
                    string.Equals(b.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase));
                if (balance is null || balance.Amount < amount)
                {
                    return ValueTask.FromResult<WalletSnapshot?>(null);
                }
            }

            // All checks passed — apply debits.
            List<WalletBalance> balances = current.Balances.ToList();
            List<WalletLedgerEntry> ledger = current.RecentEntries.ToList();

            foreach ((string currencyCode, int amount, string reasonCode) in debits)
            {
                if (amount <= 0)
                {
                    continue;
                }

                int index = balances.FindIndex(b =>
                    string.Equals(b.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase));
                if (index >= 0)
                {
                    WalletBalance existing = balances[index];
                    balances[index] = existing with { Amount = existing.Amount - amount };
                }

                ledger.Insert(0, new WalletLedgerEntry(currencyCode, -amount, reasonCode, DateTime.UtcNow));
            }

            WalletSnapshot updated = new(
                characterId,
                balances,
                ledger.OrderByDescending(e => e.CreatedAtUtc).Take(20).ToArray());

            _store.Wallets[characterId] = updated;
            return ValueTask.FromResult<WalletSnapshot?>(updated);
        }
    }

    public ValueTask<WalletSnapshot> ApplyCreditAsync(
        CharacterId characterId,
        string currencyCode,
        int amount,
        string reasonCode,
        CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            _store.Wallets.TryGetValue(characterId, out WalletSnapshot? current);
            current ??= new WalletSnapshot(characterId, [], []);

            List<WalletBalance> balances = current.Balances.ToList();
            List<WalletLedgerEntry> ledger = current.RecentEntries.ToList();

            int index = balances.FindIndex(b =>
                string.Equals(b.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
            {
                WalletBalance existing = balances[index];
                balances[index] = existing with { Amount = existing.Amount + amount };
            }
            else
            {
                balances.Add(new WalletBalance(currencyCode, amount));
            }

            ledger.Insert(0, new WalletLedgerEntry(currencyCode, amount, reasonCode, DateTime.UtcNow));

            WalletSnapshot updated = new(
                characterId,
                balances,
                ledger.OrderByDescending(e => e.CreatedAtUtc).Take(20).ToArray());

            _store.Wallets[characterId] = updated;
            return ValueTask.FromResult(updated);
        }
    }
}
