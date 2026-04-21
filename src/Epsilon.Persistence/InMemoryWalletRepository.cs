using Epsilon.CoreGame;

namespace Epsilon.Persistence;

internal sealed class InMemoryWalletRepository : IWalletRepository
{
    private readonly InMemoryHotelStore _store;

    public InMemoryWalletRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<WalletSnapshot?> GetByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        _store.Wallets.TryGetValue(characterId, out WalletSnapshot? wallet);
        return ValueTask.FromResult(wallet);
    }
}
