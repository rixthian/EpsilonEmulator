namespace Epsilon.Auth;

public interface IAuthenticator
{
    ValueTask<AuthenticationResult> AuthenticateAsync(
        AuthenticationRequest request,
        CancellationToken cancellationToken = default);
}

