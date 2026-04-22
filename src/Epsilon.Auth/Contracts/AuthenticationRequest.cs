namespace Epsilon.Auth;

public sealed record AuthenticationRequest(
    string LoginName,
    string PlainTextSecret,
    string RemoteAddress);

