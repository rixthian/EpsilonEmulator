namespace Epsilon.Auth;

public sealed record AuthenticationResult(
    bool Succeeded,
    string? FailureCode,
    SessionTicket? Session);

