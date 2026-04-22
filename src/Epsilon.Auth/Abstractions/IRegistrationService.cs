namespace Epsilon.Auth;

public interface IRegistrationService
{
    ValueTask<RegistrationResult> RegisterAsync(
        RegistrationRequest request,
        CancellationToken cancellationToken = default);
}
