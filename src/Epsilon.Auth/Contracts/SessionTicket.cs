namespace Epsilon.Auth;

public sealed record SessionTicket(
    Guid SessionId,
    long AccountId,
    long CharacterId,
    string Ticket,
    string RemoteAddress,
    DateTime CreatedAtUtc,
    DateTime LastSeenAtUtc,
    DateTime ExpiresAtUtc);
