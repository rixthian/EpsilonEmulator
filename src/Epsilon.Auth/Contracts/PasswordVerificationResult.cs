namespace Epsilon.Auth;

public sealed record PasswordVerificationResult(
    bool Succeeded,
    bool RequiresRehash,
    PasswordHashRecord? UpgradedHash);
