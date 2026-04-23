namespace Epsilon.Gateway;

public sealed record VerifyWalletChallengeInput(
    string ChallengeId,
    string Signature);
