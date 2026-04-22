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

    public ValueTask<SessionTicket?> TouchAsync(
        string ticket,
        string? remoteAddress = null,
        CancellationToken cancellationToken = default)
    {
        while (true)
        {
            if (!_sessions.TryGetValue(ticket, out SessionTicket? current))
            {
                return ValueTask.FromResult<SessionTicket?>(null);
            }

            SessionTicket updated = current with
            {
                LastSeenAtUtc = DateTime.UtcNow,
                RemoteAddress = string.IsNullOrWhiteSpace(remoteAddress) ? current.RemoteAddress : remoteAddress
            };

            if (_sessions.TryUpdate(ticket, updated, current))
            {
                return ValueTask.FromResult<SessionTicket?>(updated);
            }
        }
    }

    public ValueTask<bool> RevokeAsync(string ticket, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(_sessions.TryRemove(ticket, out _));
    }
}
