namespace Epsilon.CoreGame;

public sealed record WalletChallengeSnapshot(
    string ChallengeId,
    CharacterId CharacterId,
    string WalletAddress,
    string WalletProvider,
    string Nonce,
    string MessageToSign,
    string VerificationMode,
    string DeveloperHint,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc);
