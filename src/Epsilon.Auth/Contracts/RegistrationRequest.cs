namespace Epsilon.Auth;

public sealed record RegistrationRequest(
    string Username,
    string Email,
    string Password);
