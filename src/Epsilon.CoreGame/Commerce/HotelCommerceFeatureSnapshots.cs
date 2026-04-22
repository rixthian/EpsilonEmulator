using Epsilon.Content;

namespace Epsilon.CoreGame;

public sealed record VoucherSnapshot(
    VoucherDefinition Definition);

public sealed record CollectibleSnapshot(
    CollectibleDefinition Definition);

public sealed record EcotronRewardSnapshot(
    EcotronRewardDefinition Definition,
    string? ItemPublicName);

public sealed record HotelCommerceFeatureSnapshot(
    IReadOnlyList<VoucherSnapshot> Vouchers,
    IReadOnlyList<CollectibleSnapshot> Collectibles,
    IReadOnlyList<EcotronRewardSnapshot> EcotronRewards);
