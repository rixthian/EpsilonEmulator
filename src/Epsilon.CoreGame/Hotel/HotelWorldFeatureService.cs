using Epsilon.Content;
using Epsilon.Games;
using Epsilon.Rooms;

namespace Epsilon.CoreGame;

public sealed class HotelWorldFeatureService : IHotelWorldFeatureService
{
    private readonly IGameDefinitionRepository _gameDefinitionRepository;
    private readonly IGameVenueRepository _gameVenueRepository;
    private readonly INavigatorPublicRoomRepository _navigatorPublicRoomRepository;
    private readonly IPublicRoomBehaviorRepository _publicRoomBehaviorRepository;

    public HotelWorldFeatureService(
        IGameDefinitionRepository gameDefinitionRepository,
        IGameVenueRepository gameVenueRepository,
        INavigatorPublicRoomRepository navigatorPublicRoomRepository,
        IPublicRoomBehaviorRepository publicRoomBehaviorRepository)
    {
        _gameDefinitionRepository = gameDefinitionRepository;
        _gameVenueRepository = gameVenueRepository;
        _navigatorPublicRoomRepository = navigatorPublicRoomRepository;
        _publicRoomBehaviorRepository = publicRoomBehaviorRepository;
    }

    public async ValueTask<GameCatalogSnapshot> GetGameCatalogAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<GameDefinition> games = await _gameDefinitionRepository.GetAllAsync(cancellationToken);
        IReadOnlyList<GameVenueDefinition> venues = await _gameVenueRepository.GetAllAsync(cancellationToken);
        return new GameCatalogSnapshot(games, venues);
    }

    public async ValueTask<PublicRoomBehaviorSnapshot?> GetPublicRoomBehaviorSnapshotAsync(
        int entryId,
        CancellationToken cancellationToken = default)
    {
        NavigatorPublicRoomDefinition? entry =
            await _navigatorPublicRoomRepository.GetByEntryIdAsync(entryId, cancellationToken);
        if (entry is null)
        {
            return null;
        }

        IReadOnlyList<PublicRoomBehaviorDefinition> behaviors =
            await _publicRoomBehaviorRepository.GetByAssetPackageKeyAsync(entry.AssetPackageKey, cancellationToken);

        return new PublicRoomBehaviorSnapshot(entry, behaviors);
    }
}
