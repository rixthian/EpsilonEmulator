using Epsilon.Content;

namespace Epsilon.CoreGame;

public sealed class HotelCommerceFeatureService : IHotelCommerceFeatureService
{
    private readonly IVoucherRepository _voucherRepository;
    private readonly ICollectibleRepository _collectibleRepository;
    private readonly IEcotronRewardRepository _ecotronRewardRepository;
    private readonly IItemDefinitionRepository _itemDefinitionRepository;

    public HotelCommerceFeatureService(
        IVoucherRepository voucherRepository,
        ICollectibleRepository collectibleRepository,
        IEcotronRewardRepository ecotronRewardRepository,
        IItemDefinitionRepository itemDefinitionRepository)
    {
        _voucherRepository = voucherRepository;
        _collectibleRepository = collectibleRepository;
        _ecotronRewardRepository = ecotronRewardRepository;
        _itemDefinitionRepository = itemDefinitionRepository;
    }

    public async ValueTask<HotelCommerceFeatureSnapshot> GetSnapshotAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<VoucherDefinition> vouchers =
            await _voucherRepository.GetEnabledAsync(cancellationToken);
        IReadOnlyList<CollectibleDefinition> collectibles =
            await _collectibleRepository.GetVisibleAsync(cancellationToken);
        IReadOnlyList<EcotronRewardDefinition> ecotronRewards =
            await _ecotronRewardRepository.GetVisibleAsync(cancellationToken);

        VoucherSnapshot[] voucherSnapshots =
            vouchers.Select(definition => new VoucherSnapshot(definition)).ToArray();
        CollectibleSnapshot[] collectibleSnapshots =
            collectibles.Select(definition => new CollectibleSnapshot(definition)).ToArray();

        List<EcotronRewardSnapshot> ecotronSnapshots = new(ecotronRewards.Count);
        foreach (EcotronRewardDefinition reward in ecotronRewards.OrderBy(candidate => candidate.RewardTier))
        {
            ItemDefinition? itemDefinition =
                await _itemDefinitionRepository.GetByIdAsync(reward.ItemDefinitionId, cancellationToken);
            ecotronSnapshots.Add(new EcotronRewardSnapshot(
                reward,
                itemDefinition?.PublicName));
        }

        return new HotelCommerceFeatureSnapshot(voucherSnapshots, collectibleSnapshots, ecotronSnapshots);
    }
}
