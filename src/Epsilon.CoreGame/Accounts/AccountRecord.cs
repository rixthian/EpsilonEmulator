namespace Epsilon.CoreGame;

public sealed record AccountRecord(
    AccountId AccountId,
    string Email,
    string PasswordHashJson,
    DateTime CreatedAtUtc);
