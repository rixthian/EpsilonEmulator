namespace Epsilon.CoreGame;

public interface IWalletChallengeService
{
    ValueTask<WalletChallengeSnapshot> IssueAsync(
        CharacterId characterId,
        string walletAddress,
        string walletProvider,
        CancellationToken cancellationToken = default);

    ValueTask<WalletLinkSnapshot?> VerifyAndLinkAsync(
        CharacterId characterId,
        string challengeId,
        string signature,
        CancellationToken cancellationToken = default);
}
