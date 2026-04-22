namespace Epsilon.CoreGame;

public interface IAccountRepository
{
    ValueTask<AccountRecord?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    ValueTask<AccountRecord?> GetByIdAsync(AccountId accountId, CancellationToken cancellationToken = default);
    ValueTask<AccountId> CreateAsync(string email, string passwordHashJson, CancellationToken cancellationToken = default);
}
