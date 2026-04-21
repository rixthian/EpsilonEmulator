using Epsilon.Rooms;

namespace Epsilon.CoreGame;

public sealed class SupportCenterService : ISupportCenterService
{
    private readonly ISupportCenterRepository _supportCenterRepository;
    private readonly ICharacterProfileRepository _characterProfileRepository;
    private readonly IRoomRepository _roomRepository;

    public SupportCenterService(
        ISupportCenterRepository supportCenterRepository,
        ICharacterProfileRepository characterProfileRepository,
        IRoomRepository roomRepository)
    {
        _supportCenterRepository = supportCenterRepository;
        _characterProfileRepository = characterProfileRepository;
        _roomRepository = roomRepository;
    }

    public async ValueTask<SupportCenterSnapshot> GetSnapshotAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<SupportTopicCategory> categories = await _supportCenterRepository.GetCategoriesAsync(cancellationToken);
        IReadOnlyList<SupportTopicEntry> topics = await _supportCenterRepository.GetTopicsAsync(cancellationToken);
        IReadOnlyList<SupportCallTicket> tickets = await _supportCenterRepository.GetTicketsAsync(cancellationToken);

        return new SupportCenterSnapshot(
            categories,
            topics.Where(topic => topic.IsImportant).OrderBy(topic => topic.Title, StringComparer.OrdinalIgnoreCase).ToArray(),
            topics.Where(topic => topic.IsKnownIssue).OrderBy(topic => topic.Title, StringComparer.OrdinalIgnoreCase).ToArray(),
            tickets.Where(ticket => ticket.Status is SupportCallStatus.Open or SupportCallStatus.Picked).OrderByDescending(ticket => ticket.CreatedAtUtc).ToArray());
    }

    public async ValueTask<SupportCallResult> CreateCallAsync(
        SupportCallRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return new SupportCallResult(false, "Support call message cannot be empty.", null);
        }

        CharacterProfile? sender = await _characterProfileRepository.GetByIdAsync(request.SenderCharacterId, cancellationToken);
        if (sender is null)
        {
            return new SupportCallResult(false, "Support call sender could not be resolved.", null);
        }

        string roomName = string.Empty;
        if (request.RoomId is RoomId roomId)
        {
            Rooms.RoomDefinition? room = await _roomRepository.GetByIdAsync(roomId, cancellationToken);
            roomName = room?.Name ?? string.Empty;
        }

        SupportCallTicket ticket = await _supportCenterRepository.CreateTicketAsync(request, roomName, cancellationToken);
        return new SupportCallResult(true, "Support call created.", ticket);
    }
}
