namespace Epsilon.CoreGame;

public interface IWalletLinkRepository
{
    ValueTask<IReadOnlyList<WalletLinkSnapshot>> GetByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);

    ValueTask StoreAsync(
        WalletLinkSnapshot link,
        CancellationToken cancellationToken = default);
}
