using Epsilon.CoreGame;

namespace Epsilon.Persistence;

internal sealed class InMemoryAccountRepository : IAccountRepository
{
    private readonly InMemoryHotelStore _store;
    private readonly object _sync = new();

    public InMemoryAccountRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<AccountRecord?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        AccountRecord? account = _store.Accounts.Values.FirstOrDefault(candidate =>
            string.Equals(candidate.Email, email, StringComparison.OrdinalIgnoreCase));
        return ValueTask.FromResult(account);
    }

    public ValueTask<AccountRecord?> GetByIdAsync(AccountId accountId, CancellationToken cancellationToken = default)
    {
        _store.Accounts.TryGetValue(accountId, out AccountRecord? account);
        return ValueTask.FromResult(account);
    }

    public ValueTask<AccountId> CreateAsync(string email, string passwordHashJson, CancellationToken cancellationToken = default)
    {
        AccountId newId;
        AccountRecord record;

        lock (_sync)
        {
            newId = new AccountId(_store.NextAccountId++);
            record = new AccountRecord(newId, email, passwordHashJson, DateTime.UtcNow);
            _store.Accounts[newId] = record;
        }

        return ValueTask.FromResult(newId);
    }
}
