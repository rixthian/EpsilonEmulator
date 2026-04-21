using Epsilon.Content;
using Epsilon.Rooms;

namespace Epsilon.CoreGame;

public sealed record CharacterHotelSnapshot(
    CharacterProfile Profile,
    IReadOnlyList<SubscriptionRecord> Subscriptions,
    IReadOnlyList<PetProfile> Pets);

public sealed record RoomItemSnapshot(
    RoomItemState Item,
    ItemDefinition? Definition);

public sealed record RoomHotelSnapshot(
    RoomDefinition Room,
    RoomLayoutDefinition? Layout,
    IReadOnlyList<RoomItemSnapshot> Items);

public sealed record PublicRoomHotelSnapshot(
    NavigatorPublicRoomDefinition Entry,
    PublicRoomAssetPackageDefinition? AssetPackage,
    RoomHotelSnapshot? Room);
