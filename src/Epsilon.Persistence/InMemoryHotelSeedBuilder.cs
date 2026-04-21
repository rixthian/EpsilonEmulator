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

        store.Wallets[characterId] = new WalletSnapshot(
            characterId,
            [
                new WalletBalance("credits", 5000),
                new WalletBalance("duckets", 750),
                new WalletBalance("diamonds", 120)
            ],
            [
                new WalletLedgerEntry("credits", 2500, "starter_grant", DateTime.UtcNow.AddDays(-7)),
                new WalletLedgerEntry("duckets", 250, "daily_reward", DateTime.UtcNow.AddDays(-1)),
                new WalletLedgerEntry("diamonds", 20, "seasonal_reward", DateTime.UtcNow.AddHours(-8))
            ]);

        store.Pets[characterId] =
        [
            new PetProfile(new PetId(1), characterId, homeRoomId, "Orbit", 0, "000", "FFFFFF", 120, 85, 100, 5, 3)
        ];

        store.MessengerContacts[characterId] =
        [
            new MessengerContact(new CharacterId(2), "delta", "Building room systems", true, 0, "friend"),
            new MessengerContact(new CharacterId(3), "nova", "Catalog curator", false, 1, "favorite")
        ];

        store.PendingMessengerRequests[characterId] =
        [
            new MessengerRequest(1, new CharacterId(4), "atlas", "hd-180-1.ch-215-92.lg-275-82.sh-300-62", DateTime.UtcNow.AddHours(-3))
        ];

        store.BadgeAssignments[characterId] =
        [
            new BadgeAssignment("ACH_Login1", 1, true),
            new BadgeAssignment("ADM", 2, false),
            new BadgeAssignment("CLUB1", 3, false)
        ];

        store.AchievementProgress[characterId] =
        [
            new AchievementProgress("login_streak", 3, 14, 20, DateTime.UtcNow.AddHours(-12)),
            new AchievementProgress("room_visitor", 2, 45, 75, DateTime.UtcNow.AddDays(-1)),
            new AchievementProgress("catalog_collector", 1, 6, 10, DateTime.UtcNow.AddDays(-2))
        ];

        store.SupportCategories.AddRange(
        [
            new SupportTopicCategory(1, "Technical issues"),
            new SupportTopicCategory(2, "Harassment and abuse"),
            new SupportTopicCategory(3, "Account and safety")
        ]);

        store.SupportTopics.AddRange(
        [
            new SupportTopicEntry(1, "How to report a bug", "Use the support button and include room, steps, and timing.", 1, false, true),
            new SupportTopicEntry(2, "Room movement issue", "If your avatar gets stuck, leave and re-enter the room.", 1, true, false),
            new SupportTopicEntry(3, "How to report harassment", "Use the support button and select the abuse category.", 2, false, true)
        ]);

        store.ChatCommands[characterId] =
        [
            new ChatCommandDefinition("help", "Show the available command list.", ChatCommandScope.Player, false, ["commands"]),
            new ChatCommandDefinition("coords", "Show the current room coordinates.", ChatCommandScope.Player, true, []),
            new ChatCommandDefinition("userinfo", "Show the current actor runtime information.", ChatCommandScope.Player, true, []),
            new ChatCommandDefinition("sign", "Set the visible sign/status value.", ChatCommandScope.Player, true, []),
            new ChatCommandDefinition("carry", "Set the carried hand item for testing.", ChatCommandScope.Player, true, []),
            new ChatCommandDefinition("pickall", "Return every floor item in the current room to storage.", ChatCommandScope.RoomModerator, true, []),
            new ChatCommandDefinition("roommute", "Mute or unmute the current room chat.", ChatCommandScope.RoomModerator, true, []),
            new ChatCommandDefinition("ha", "Broadcast a hotel-wide alert.", ChatCommandScope.HotelModerator, false, ["hotelalert"])
        ];

        store.AccessCapabilities.AddRange(
        [
            new AccessCapability("hotel.alert", "Send a hotel-wide alert."),
            new AccessCapability("hotel.modtool", "Access moderation tooling."),
            new AccessCapability("room.mute", "Mute or unmute a room."),
            new AccessCapability("room.pick_all", "Return all room items to storage."),
            new AccessCapability("catalog.reload", "Reload catalog content."),
            new AccessCapability("housekeeping.access", "Access housekeeping surfaces.")
        ]);

        store.StaffRoleDefinitions.AddRange(
        [
            new StaffRoleDefinition("player", "Player", 0, []),
            new StaffRoleDefinition("hobba", "Hobba", 20, ["room.mute"]),
            new StaffRoleDefinition("lince", "Lince", 40, ["room.mute", "hotel.modtool"]),
            new StaffRoleDefinition("moderator", "Moderator", 60, ["room.mute", "hotel.modtool", "hotel.alert"]),
            new StaffRoleDefinition("administrator", "Administrator", 90, ["room.mute", "hotel.modtool", "hotel.alert", "catalog.reload", "housekeeping.access"]),
            new StaffRoleDefinition("owner", "Owner", 100, ["room.mute", "hotel.modtool", "hotel.alert", "catalog.reload", "housekeeping.access", "room.pick_all"])
        ]);

        store.StaffRoleAssignments[characterId] =
        [
            new StaffRoleAssignment(characterId, "owner", true, DateTime.UtcNow.AddMonths(-6)),
            new StaffRoleAssignment(characterId, "administrator", false, DateTime.UtcNow.AddMonths(-7))
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

        store.ClientPackages["flash_release63_baseline"] = new ClientPackageManifest(
            PackageKey: "flash_release63_baseline",
            PackageFamily: "flash_compatibility",
            VersionKey: "release63",
            EntryAssetPath: "clients/release63/index.html",
            AssetBasePath: "clients/release63/assets",
            ExternalVariableKeys: ["release63.variables.core", "release63.variables.ui"],
            ExternalTextKeys: ["release63.texts.core", "release63.texts.catalog"],
            PublicRoomAssetPackageKeys: ["welcome_lobby_core"],
            FeatureFlags: ["catalog", "messenger", "navigator", "room_runtime"]);

        store.ClientPackages["modern_web_runtime_alpha"] = new ClientPackageManifest(
            PackageKey: "modern_web_runtime_alpha",
            PackageFamily: "modern_web_runtime",
            VersionKey: "alpha1",
            EntryAssetPath: "clients/web-alpha/index.html",
            AssetBasePath: "clients/web-alpha/assets",
            ExternalVariableKeys: ["web-alpha.variables.core"],
            ExternalTextKeys: ["web-alpha.texts.core"],
            PublicRoomAssetPackageKeys: ["welcome_lobby_core"],
            FeatureFlags: ["catalog", "messenger", "navigator", "room_runtime", "activity_runtime"]);

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

        store.RoomActors[homeRoomId] =
        [
            new RoomActorState(
                ActorId: characterId.Value,
                ActorKind: RoomActorKind.Player,
                DisplayName: "epsilon",
                Position: new RoomCoordinate(6, 4, 0),
                BodyRotation: 2,
                HeadRotation: 2,
                IsTyping: false,
                IsWalking: false,
                IsSitting: false,
                IsLaying: false,
                CarryItem: new CarryItemState(4, "Coffee", DateTime.UtcNow.AddMinutes(2)),
                Goal: new MovementGoal(8, 4),
                StatusEntries:
                [
                    new ActorStatusEntry("mv", "8,4,0"),
                    new ActorStatusEntry("sign", "3")
                ]),
            new RoomActorState(
                ActorId: 1,
                ActorKind: RoomActorKind.Pet,
                DisplayName: "Orbit",
                Position: new RoomCoordinate(7, 5, 0),
                BodyRotation: 4,
                HeadRotation: 4,
                IsTyping: false,
                IsWalking: true,
                IsSitting: false,
                IsLaying: false,
                CarryItem: null,
                Goal: new MovementGoal(9, 5),
                StatusEntries:
                [
                    new ActorStatusEntry("mv", "9,5,0")
                ])
        ];

        store.RoomActivities[homeRoomId] = new RoomActivitySnapshot(
            RoomActivityKind.None,
            false,
            "idle",
            [],
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase));

        store.RoomChatPolicies[homeRoomId] = new RoomChatPolicySnapshot(
            IsMuted: false,
            UsesChatQueue: true,
            MaxMessageLength: 100,
            FloodWindowSeconds: 4,
            MaxMessagesPerWindow: 4);

        store.RoomChatMessages[homeRoomId] =
        [
            new RoomChatMessage(1, homeRoomId, characterId.Value, "epsilon", "Welcome to the room runtime slice.", RoomChatMessageKind.System, DateTime.UtcNow.AddMinutes(-5))
        ];

        store.Advertisements.AddRange(
        [
            new HotelAdvertisement(
                AdvertisementId: 1,
                PlacementKey: "landing",
                ImageAssetPath: "ads/launch_campaign_spring.png",
                ClickThroughUrl: "https://epsilon.example/hotel-news/spring",
                ViewCount: 24,
                ViewLimit: 1000,
                IsActive: true),
            new HotelAdvertisement(
                AdvertisementId: 2,
                PlacementKey: "room_billboard",
                ImageAssetPath: "ads/room_billboard_builder.png",
                ClickThroughUrl: "https://epsilon.example/community/builders",
                ViewCount: 85,
                ViewLimit: 0,
                IsActive: true)
        ]);

        return store;
    }
}
