namespace Epsilon.CoreGame;

public sealed record WalletLinkSnapshot(
    CharacterId CharacterId,
    string WalletAddress,
    string WalletProvider,
    string VerificationMode,
    bool IsPrimary,
    DateTime LinkedAtUtc,
    DateTime LastVerifiedAtUtc);
