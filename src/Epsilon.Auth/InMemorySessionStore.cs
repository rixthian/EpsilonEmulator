using System.Collections.Concurrent;

namespace Epsilon.Auth;

public sealed class InMemorySessionStore : ISessionStore
{
    private readonly ConcurrentDictionary<string, SessionTicket> _sessions = new(StringComparer.Ordinal);

    public ValueTask StoreAsync(SessionTicket ticket, CancellationToken cancellationToken = default)
    {
        _sessions[ticket.Ticket] = ticket;
        return ValueTask.CompletedTask;
    }

    public ValueTask<SessionTicket?> FindByTicketAsync(string ticket, CancellationToken cancellationToken = default)
    {
        _sessions.TryGetValue(ticket, out SessionTicket? session);
        return ValueTask.FromResult(session);
    }
}

