namespace Epsilon.CoreGame;

public interface ISupportCenterRepository
{
    ValueTask<IReadOnlyList<SupportTopicCategory>> GetCategoriesAsync(
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<SupportTopicEntry>> GetTopicsAsync(
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<SupportCallTicket>> GetTicketsAsync(
        CancellationToken cancellationToken = default);

    ValueTask<SupportCallTicket> CreateTicketAsync(
        SupportCallRequest request,
        string roomName,
        CancellationToken cancellationToken = default);
}
