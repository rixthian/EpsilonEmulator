using Epsilon.CoreGame;

namespace Epsilon.Persistence;

internal sealed class InMemorySupportCenterRepository : ISupportCenterRepository
{
    private readonly InMemoryHotelStore _store;
    private long _nextTicketId;

    public InMemorySupportCenterRepository(InMemoryHotelStore store)
    {
        _store = store;
        _nextTicketId = _store.SupportCallTickets
            .Select(ticket => ticket.TicketId)
            .DefaultIfEmpty(0)
            .Max() + 1;
    }

    public ValueTask<IReadOnlyList<SupportTopicCategory>> GetCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<IReadOnlyList<SupportTopicCategory>>(_store.SupportCategories);
    }

    public ValueTask<IReadOnlyList<SupportTopicEntry>> GetTopicsAsync(
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<IReadOnlyList<SupportTopicEntry>>(_store.SupportTopics);
    }

    public ValueTask<IReadOnlyList<SupportCallTicket>> GetTicketsAsync(
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<IReadOnlyList<SupportCallTicket>>(_store.SupportCallTickets);
    }

    public ValueTask<SupportCallTicket> CreateTicketAsync(
        SupportCallRequest request,
        string roomName,
        CancellationToken cancellationToken = default)
    {
        SupportCallTicket ticket = new(
            TicketId: _nextTicketId++,
            CategoryId: request.CategoryId,
            SenderCharacterId: request.SenderCharacterId,
            ReportedCharacterId: request.ReportedCharacterId,
            ModeratorCharacterId: null,
            RoomId: request.RoomId,
            RoomName: roomName,
            Message: request.Message.Trim(),
            Score: 1,
            Status: SupportCallStatus.Open,
            CreatedAtUtc: DateTime.UtcNow);

        _store.SupportCallTickets.Add(ticket);
        return ValueTask.FromResult(ticket);
    }
}
