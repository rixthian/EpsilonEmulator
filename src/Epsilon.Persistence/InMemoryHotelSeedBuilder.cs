using Epsilon.Content;
using Epsilon.CoreGame;
using Epsilon.Rooms;

namespace Epsilon.Persistence;

internal static class InMemoryHotelSeedBuilder
{
    public static InMemoryHotelStore Build()
    {
        InMemoryHotelStore store = new();

        CharacterId characterId = new(1);
        AccountId accountId = new(1);
        RoomId homeRoomId = new(1);

        store.Characters[characterId] = new CharacterProfile(
            CharacterId: characterId,
            AccountId: accountId,
            Username: "epsilon",
            Motto: "Modern compatibility hotel",
            Figure: "hd-180-1.ch-210-66.lg-270-82.sh-290-80",
            Gender: "M",
            HomeRoomId: homeRoomId,
            CreditsBalance: 5000,
            ActivityPointsBalance: 750,
            RespectPoints: 25,
            DailyRespectPoints: 3,
            DailyPetRespectPoints: 3);

        store.Subscriptions[characterId] =
        [
            new SubscriptionRecord(characterId, SubscriptionType.Club, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow.AddDays(30)),
            new SubscriptionRecord(characterId, SubscriptionType.Vip, DateTime.UtcNow.AddDays(-5), DateTime.UtcNow.AddDays(25))
        ];

        store.Pets[characterId] =
        [
            new PetProfile(new PetId(1), characterId, homeRoomId, "Orbit", 0, "000", "FFFFFF", 120, 85, 100, 5, 3)
        ];

        store.Layouts["newbie_lobby"] = new RoomLayoutDefinition(
            LayoutCode: "newbie_lobby",
            DoorPosition: new FloorPosition(15, 7, 0),
            DoorRotation: 2,
            Heightmap:
                "xxxxxxxxxxxxxxxx\n" +
                "xxxx11111111xxxx\n" +
                "1111111111111111\n" +
                "1111111111111111\n" +
                "1111111111111111\n" +
                "1111111111111111\n" +
                "1111111111111111\n" +
                "1111111111111111",
            PublicRoomObjectSetCodes: ["hh_room_nlobby"],
            ClubOnly: false);

        store.Rooms[homeRoomId] = new RoomDefinition(
            RoomId: homeRoomId,
            RoomKind: RoomKind.Public,
            OwnerCharacterId: null,
            Name: "Welcome Lounge",
            Description: "Initial development room",
            CategoryId: 0,
            LayoutCode: "newbie_lobby",
            Settings: new RoomSettings(RoomAccessMode.Open, null, 50, true, false, false, false),
            Tags: ["welcome", "public"]);

        store.PublicRoomAssetPackages["welcome_lobby_core"] = new PublicRoomAssetPackageDefinition(
            AssetPackageKey: "welcome_lobby_core",
            AssetFamily: "public_room",
            VisualProfileKey: "default_public_room",
            BaseLayoutCode: "newbie_lobby",
            AssetLayerKeys: ["background", "props", "lighting", "entry"],
            Tags: ["welcome", "lobby"]);

        store.NavigatorPublicRooms[1] = new NavigatorPublicRoomDefinition(
            EntryId: 1,
            OrderNumber: 1,
            BannerTypeCode: "standard",
            Caption: "Hotel Reception",
            ImagePath: "welcome_lobby_banner",
            ImageKind: "internal",
            RoomId: homeRoomId,
            CategoryId: 0,
            ParentCategoryId: 0,
            AssetPackageKey: "welcome_lobby_core");

        ItemDefinitionId sofaDefinitionId = new(1001);
        ItemDefinitionId teleporterDefinitionId = new(1002);

        store.ItemDefinitions[sofaDefinitionId] = new ItemDefinition(
            sofaDefinitionId,
            PublicName: "Modern Sofa",
            InternalName: "modern_sofa",
            ItemTypeCode: "s",
            SpriteId: 3001,
            StackHeight: 1.0,
            CanStack: true,
            CanSit: true,
            IsWalkable: false,
            AllowRecycle: true,
            AllowTrade: true,
            AllowMarketplaceSell: true,
            AllowGift: true,
            AllowInventoryStack: false,
            InteractionTypeCode: "default",
            InteractionModesCount: 0);

        store.ItemDefinitions[teleporterDefinitionId] = new ItemDefinition(
            teleporterDefinitionId,
            PublicName: "Teleporter",
            InternalName: "teleporter",
            ItemTypeCode: "s",
            SpriteId: 3002,
            StackHeight: 1.0,
            CanStack: false,
            CanSit: false,
            IsWalkable: true,
            AllowRecycle: false,
            AllowTrade: true,
            AllowMarketplaceSell: true,
            AllowGift: true,
            AllowInventoryStack: false,
            InteractionTypeCode: "teleport",
            InteractionModesCount: 2);

        store.RoomItems[homeRoomId] =
        [
            new RoomItemState(
                new ItemId(5001),
                sofaDefinitionId,
                homeRoomId,
                new RoomItemPlacement(new FloorPosition(4, 4, 0), 2, null),
                "0"),
            new RoomItemState(
                new ItemId(5002),
                teleporterDefinitionId,
                homeRoomId,
                new RoomItemPlacement(new FloorPosition(8, 4, 0), 0, null),
                "0")
        ];

        return store;
    }
}
