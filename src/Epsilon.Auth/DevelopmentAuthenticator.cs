using Microsoft.Extensions.Options;

namespace Epsilon.Auth;

public sealed class DevelopmentAuthenticator : IAuthenticator
{
    private readonly AuthOptions _options;
    private readonly ISystemClock _clock;
    private readonly ITicketGenerator _ticketGenerator;
    private readonly ISessionStore _sessionStore;

    public DevelopmentAuthenticator(
        IOptions<AuthOptions> options,
        ISystemClock clock,
        ITicketGenerator ticketGenerator,
        ISessionStore sessionStore)
    {
        _options = options.Value;
        _clock = clock;
        _ticketGenerator = ticketGenerator;
        _sessionStore = sessionStore;
    }

    public async ValueTask<AuthenticationResult> AuthenticateAsync(
        AuthenticationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.LoginName))
        {
            return new AuthenticationResult(false, "login_required", null);
        }

        if (string.IsNullOrWhiteSpace(request.PlainTextSecret))
        {
            return new AuthenticationResult(false, "secret_required", null);
        }

        DateTime createdAt = _clock.UtcNow;
        SessionTicket session = new(
            Guid.NewGuid(),
            AccountId: 1,
            CharacterId: 1,
            Ticket: _ticketGenerator.Generate(_options.TicketLength),
            RemoteAddress: request.RemoteAddress,
            CreatedAtUtc: createdAt,
            ExpiresAtUtc: createdAt.AddMinutes(_options.SessionTtlMinutes));

        await _sessionStore.StoreAsync(session, cancellationToken);
        return new AuthenticationResult(true, null, session);
    }
}

