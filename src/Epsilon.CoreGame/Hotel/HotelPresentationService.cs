using Epsilon.Content;
using Epsilon.Rooms;

namespace Epsilon.CoreGame;

public sealed class HotelPresentationService : IHotelPresentationService
{
    private readonly IHotelCommerceService _hotelCommerceService;
    private readonly ICatalogCampaignRepository _catalogCampaignRepository;
    private readonly ICatalogFeatureStateRepository _catalogFeatureStateRepository;
    private readonly ICatalogOfferRepository _catalogOfferRepository;
    private readonly IEffectDefinitionRepository _effectDefinitionRepository;
    private readonly IHotelReadService _hotelReadService;
    private readonly IRoomVisualSceneRepository _roomVisualSceneRepository;

    public HotelPresentationService(
        IHotelCommerceService hotelCommerceService,
        ICatalogCampaignRepository catalogCampaignRepository,
        ICatalogFeatureStateRepository catalogFeatureStateRepository,
        ICatalogOfferRepository catalogOfferRepository,
        IEffectDefinitionRepository effectDefinitionRepository,
        IHotelReadService hotelReadService,
        IRoomVisualSceneRepository roomVisualSceneRepository)
    {
        _hotelCommerceService = hotelCommerceService;
        _catalogCampaignRepository = catalogCampaignRepository;
        _catalogFeatureStateRepository = catalogFeatureStateRepository;
        _catalogOfferRepository = catalogOfferRepository;
        _effectDefinitionRepository = effectDefinitionRepository;
        _hotelReadService = hotelReadService;
        _roomVisualSceneRepository = roomVisualSceneRepository;
    }

    public async ValueTask<CatalogLandingSnapshot> GetCatalogLandingAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CatalogCampaignDefinition> campaigns =
            await _catalogCampaignRepository.GetVisibleByCharacterIdAsync(characterId, cancellationToken);

        List<CatalogCampaignSnapshot> snapshots = [];
        foreach (CatalogCampaignDefinition campaign in campaigns.OrderBy(candidate => candidate.OrderNumber))
        {
            CatalogPageSnapshot? page = await _hotelCommerceService.GetCatalogPageAsync(
                characterId,
                campaign.CatalogPageId,
                cancellationToken);
            snapshots.Add(new CatalogCampaignSnapshot(campaign, page));
        }

        CatalogFeatureState? rareFeatureState =
            await _catalogFeatureStateRepository.GetByFeatureKeyAsync(CatalogFeatureKeys.RareOfTheWeek, cancellationToken);
        CatalogOfferDefinition? rareOffer =
            rareFeatureState is { IsEnabled: true, FeaturedOfferId: not null }
                ? await _catalogOfferRepository.GetByIdAsync(rareFeatureState.FeaturedOfferId.Value, cancellationToken)
                : null;
        CatalogOfferSnapshot? rareOfTheWeek = null;
        if (rareOffer is not null)
        {
            CatalogPageSnapshot? rarePage = await _hotelCommerceService.GetCatalogPageAsync(
                characterId,
                rareOffer.CatalogPageId,
                cancellationToken);
            rareOfTheWeek = rarePage?.Offers.FirstOrDefault(offer => offer.Offer.CatalogOfferId == rareOffer.CatalogOfferId);
        }

        CatalogPageSnapshot? ecotronPage = snapshots
            .FirstOrDefault(snapshot => string.Equals(
                snapshot.Campaign.CampaignKey,
                CatalogCampaignKeys.EcotronRevival,
                StringComparison.OrdinalIgnoreCase))
            ?.Page;

        IReadOnlyList<EffectDefinition> featuredEffects =
            await _effectDefinitionRepository.GetVisibleAsync(cancellationToken);

        return new CatalogLandingSnapshot(
            snapshots,
            rareOfTheWeek,
            ecotronPage,
            featuredEffects);
    }

    public async ValueTask<RoomVisualSnapshot?> GetRoomVisualSnapshotAsync(
        RoomId roomId,
        CancellationToken cancellationToken = default)
    {
        RoomHotelSnapshot? room = await _hotelReadService.GetRoomSnapshotAsync(roomId, cancellationToken);
        if (room is null)
        {
            return null;
        }

        RoomVisualSceneDefinition? scene = await _roomVisualSceneRepository.GetByLayoutCodeAsync(
            room.Room.LayoutCode,
            cancellationToken);

        return new RoomVisualSnapshot(
            room.Room.RoomId,
            room.Room.Name,
            scene,
            scene?.AmbientEffectKeys ?? []);
    }
}
