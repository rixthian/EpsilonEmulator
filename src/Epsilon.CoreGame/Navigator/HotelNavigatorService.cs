using Epsilon.Content;
using Epsilon.Games;
using Epsilon.Rooms;

namespace Epsilon.CoreGame;

public sealed class HotelNavigatorService : IHotelNavigatorService
{
    private readonly INavigatorPublicRoomRepository _navigatorPublicRoomRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly IGameVenueRepository _gameVenueRepository;

    public HotelNavigatorService(
        INavigatorPublicRoomRepository navigatorPublicRoomRepository,
        IRoomRepository roomRepository,
        IGameVenueRepository gameVenueRepository)
    {
        _navigatorPublicRoomRepository = navigatorPublicRoomRepository;
        _roomRepository = roomRepository;
        _gameVenueRepository = gameVenueRepository;
    }

    public async ValueTask<NavigatorSearchSnapshot> SearchPublicRoomsAsync(
        NavigatorSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<NavigatorPublicRoomDefinition> entries =
            await _navigatorPublicRoomRepository.GetAllAsync(cancellationToken);
        IReadOnlyList<GameVenueDefinition> venues =
            await _gameVenueRepository.GetAllAsync(cancellationToken);

        IEnumerable<NavigatorPublicRoomDefinition> query = entries;
        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            query = query.Where(candidate =>
                candidate.Caption.Contains(request.Query, StringComparison.OrdinalIgnoreCase) ||
                candidate.BannerTypeCode.Contains(request.Query, StringComparison.OrdinalIgnoreCase) ||
                candidate.AssetPackageKey.Contains(request.Query, StringComparison.OrdinalIgnoreCase));
        }

        if (request.RecommendedOnly)
        {
            HashSet<RoomId> recommendedRoomIds = venues
                .Where(candidate => candidate.IsRecommended)
                .Select(candidate => candidate.RoomId)
                .ToHashSet();
            query = query.Where(candidate => recommendedRoomIds.Contains(candidate.RoomId));
        }

        List<NavigatorPublicRoomSnapshot> results = [];
        foreach (NavigatorPublicRoomDefinition entry in query.OrderBy(candidate => candidate.OrderNumber))
        {
            RoomDefinition? room = await _roomRepository.GetByIdAsync(entry.RoomId, cancellationToken);
            results.Add(new NavigatorPublicRoomSnapshot(entry, room));
        }

        return new NavigatorSearchSnapshot(results);
    }
}
