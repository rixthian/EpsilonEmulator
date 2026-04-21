namespace Epsilon.Auth;

public interface ISessionStore
{
    ValueTask StoreAsync(SessionTicket ticket, CancellationToken cancellationToken = default);
    ValueTask<SessionTicket?> FindByTicketAsync(string ticket, CancellationToken cancellationToken = default);
}

