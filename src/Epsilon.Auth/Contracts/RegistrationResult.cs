namespace Epsilon.Auth;

public sealed record RegistrationResult(
    bool Succeeded,
    string? FailureCode,
    long? AccountId,
    long? CharacterId);
