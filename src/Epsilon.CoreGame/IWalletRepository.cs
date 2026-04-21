namespace Epsilon.CoreGame;

public interface IWalletRepository
{
    ValueTask<WalletSnapshot?> GetByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);
}
