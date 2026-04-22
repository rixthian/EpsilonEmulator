using Microsoft.Extensions.Options;

namespace Epsilon.Auth;

public sealed class DevelopmentAuthenticator : IAuthenticator
{
    private readonly AuthOptions _options;
    private readonly ISystemClock _clock;
    private readonly ITicketGenerator _ticketGenerator;
    private readonly ISessionStore _sessionStore;
    private readonly IDevLoginCharacterResolver _characterResolver;

    public DevelopmentAuthenticator(
        IOptions<AuthOptions> options,
        ISystemClock clock,
        ITicketGenerator ticketGenerator,
        ISessionStore sessionStore,
        IDevLoginCharacterResolver characterResolver)
    {
        _options = options.Value;
        _clock = clock;
        _ticketGenerator = ticketGenerator;
        _sessionStore = sessionStore;
        _characterResolver = characterResolver;
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

        // In development mode any non-empty secret is accepted; the character
        // must exist in the hotel store so sessions are always valid identities.
        DevLoginIdentity? identity = await _characterResolver.ResolveAsync(request.LoginName, cancellationToken);
        if (identity is null)
        {
            return new AuthenticationResult(false, "character_not_found", null);
        }

        DateTime createdAt = _clock.UtcNow;
        SessionTicket session = new(
            Guid.NewGuid(),
            AccountId: identity.AccountId,
            CharacterId: identity.CharacterId,
            Ticket: _ticketGenerator.Generate(_options.TicketLength),
            RemoteAddress: request.RemoteAddress,
            CreatedAtUtc: createdAt,
            LastSeenAtUtc: createdAt,
            ExpiresAtUtc: createdAt.AddMinutes(_options.SessionTtlMinutes));

        await _sessionStore.StoreAsync(session, cancellationToken);
        return new AuthenticationResult(true, null, session);
    }
}
