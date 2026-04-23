namespace Epsilon.Gateway;

public sealed record CreateWalletChallengeInput(
    string WalletAddress,
    string? WalletProvider);
