namespace Epsilon.CoreGame;

public sealed record SupportCallResult(
    bool Succeeded,
    string Detail,
    SupportCallTicket? Ticket);
