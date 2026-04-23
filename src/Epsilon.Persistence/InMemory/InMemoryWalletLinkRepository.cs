using Epsilon.CoreGame;

namespace Epsilon.Persistence;

internal sealed class InMemoryWalletLinkRepository : IWalletLinkRepository
{
    private readonly InMemoryHotelStore _store;
    private readonly object _sync = new();

    public InMemoryWalletLinkRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<IReadOnlyList<WalletLinkSnapshot>> GetByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (!_store.WalletLinks.TryGetValue(characterId, out List<WalletLinkSnapshot>? links))
            {
                return ValueTask.FromResult<IReadOnlyList<WalletLinkSnapshot>>([]);
            }

            return ValueTask.FromResult<IReadOnlyList<WalletLinkSnapshot>>(links.ToArray());
        }
    }

    public ValueTask StoreAsync(
        WalletLinkSnapshot link,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (!_store.WalletLinks.TryGetValue(link.CharacterId, out List<WalletLinkSnapshot>? links))
            {
                links = [];
                _store.WalletLinks[link.CharacterId] = links;
            }

            int existingIndex = links.FindIndex(candidate =>
                string.Equals(candidate.WalletAddress, link.WalletAddress, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(candidate.WalletProvider, link.WalletProvider, StringComparison.OrdinalIgnoreCase));

            if (link.IsPrimary)
            {
                for (int index = 0; index < links.Count; index++)
                {
                    links[index] = links[index] with { IsPrimary = false };
                }
            }

            if (existingIndex >= 0)
            {
                links[existingIndex] = link;
            }
            else
            {
                links.Add(link);
            }
        }

        return ValueTask.CompletedTask;
    }
}
