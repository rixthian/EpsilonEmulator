using Epsilon.Content;
using Epsilon.CoreGame;
using Epsilon.Games;
using Epsilon.Rooms;

namespace Epsilon.Persistence;

internal static class InMemoryHotelSeedBuilder
{
    public static InMemoryHotelStore Build()
    {
        InMemoryHotelStore store = new();

        CharacterId characterId = new(1);
        CharacterId roomGuardianCharacterId = new(2);
        CharacterId linceCharacterId = new(3);
        CharacterId moderatorCharacterId = new(4);
        CharacterId administratorCharacterId = new(5);
        CharacterId managerCharacterId = new(6);
        CharacterId playerCharacterId = new(7);
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

        store.Characters[roomGuardianCharacterId] = new CharacterProfile(
            roomGuardianCharacterId,
            new AccountId(2),
            "delta",
            "Room guardian",
            "hd-180-1.ch-210-66.lg-270-82.sh-290-80",
            "M",
            homeRoomId,
            2500,
            250,
            5,
            3,
            3);

        store.Characters[linceCharacterId] = new CharacterProfile(
            linceCharacterId,
            new AccountId(3),
            "nova",
            "Hotel guardian",
            "hd-600-1.ch-665-92.lg-715-82.sh-725-80",
            "F",
            homeRoomId,
            3000,
            400,
            8,
            3,
            3);

        store.Characters[moderatorCharacterId] = new CharacterProfile(
            moderatorCharacterId,
            new AccountId(4),
            "atlas",
            "Moderator on duty",
            "hd-180-1.ch-215-92.lg-275-82.sh-300-62",
            "M",
            homeRoomId,
            3500,
            500,
            12,
            3,
            3);

        store.Characters[administratorCharacterId] = new CharacterProfile(
            administratorCharacterId,
            new AccountId(5),
            "pixel",
            "Administrator",
            "hd-600-1.ch-630-92.lg-695-82.sh-725-62",
            "F",
            homeRoomId,
            4000,
            600,
            15,
            3,
            3);

        store.Characters[managerCharacterId] = new CharacterProfile(
            managerCharacterId,
            new AccountId(6),
            "ember",
            "Operations manager",
            "hd-190-1.ch-255-66.lg-280-82.sh-290-80",
            "M",
            homeRoomId,
            4500,
            650,
            18,
            3,
            3);

        store.Characters[playerCharacterId] = new CharacterProfile(
            playerCharacterId,
            new AccountId(7),
            "vector",
            "Regular user",
            "hd-180-1.ch-210-66.lg-270-82.sh-290-80",
            "M",
            homeRoomId,
            1200,
            100,
            2,
            3,
            3);

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

        store.Wallets[roomGuardianCharacterId] = new WalletSnapshot(
            roomGuardianCharacterId,
            [new WalletBalance("credits", 1000), new WalletBalance("duckets", 100)],
            []);
        store.Wallets[linceCharacterId] = new WalletSnapshot(
            linceCharacterId,
            [new WalletBalance("credits", 1000), new WalletBalance("duckets", 100)],
            []);
        store.Wallets[moderatorCharacterId] = new WalletSnapshot(
            moderatorCharacterId,
            [new WalletBalance("credits", 1000), new WalletBalance("duckets", 100)],
            []);
        store.Wallets[administratorCharacterId] = new WalletSnapshot(
            administratorCharacterId,
            [new WalletBalance("credits", 1000), new WalletBalance("duckets", 100)],
            []);
        store.Wallets[managerCharacterId] = new WalletSnapshot(
            managerCharacterId,
            [new WalletBalance("credits", 1000), new WalletBalance("duckets", 100)],
            []);
        store.Wallets[playerCharacterId] = new WalletSnapshot(
            playerCharacterId,
            [new WalletBalance("credits", 1000), new WalletBalance("duckets", 100)],
            []);

        store.InterfacePreferences[characterId] = new CharacterInterfacePreference(
            CharacterId: characterId,
            LanguageCode: "en",
            UpdatedAt: DateTimeOffset.UtcNow.AddDays(-5),
            UpdatedBy: "seed");

        store.InterfaceLanguages.AddRange(EmbeddedSeedContentLoader.LoadInterfaceLanguages());
        foreach (ItemDefinition itemDefinition in EmbeddedSeedContentLoader.LoadItemDefinitions())
        {
            store.ItemDefinitions[itemDefinition.ItemDefinitionId] = itemDefinition;
        }

        ItemDefinitionId sofaDefinitionId = new(1001);
        ItemDefinitionId teleporterDefinitionId = new(1002);
        ItemDefinitionId plantDefinitionId = new(1003);

        store.BadgeDefinitions.AddRange(EmbeddedSeedContentLoader.LoadBadgeDefinitions());

        store.InventoryItems[characterId] = [];

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

        store.ChatCommandCatalog.AddRange(
        [
            new ChatCommandDefinition("help", "Show the available command list.", ChatCommandScope.Player, false, ["commands"], null),
            new ChatCommandDefinition("coords", "Show the current room coordinates.", ChatCommandScope.Player, true, [], null),
            new ChatCommandDefinition("roomid", "Show the current room identity and type.", ChatCommandScope.Player, true, ["rid"], null),
            new ChatCommandDefinition("roomstats", "Show actor, item, and chat counts for the current room.", ChatCommandScope.Player, true, ["rstats"], null),
            new ChatCommandDefinition("chooser", "List the players currently present in the room.", ChatCommandScope.Player, true, [], null),
            new ChatCommandDefinition("furni", "List the furni currently present in the room.", ChatCommandScope.Player, true, [], null),
            new ChatCommandDefinition("userinfo", "Show the current actor runtime information.", ChatCommandScope.Player, true, ["whoami"], null),
            new ChatCommandDefinition("sign", "Set the visible sign/status value.", ChatCommandScope.Player, true, [], null),
            new ChatCommandDefinition("carry", "Set the carried hand item for testing.", ChatCommandScope.Player, true, ["handitem"], null),
            new ChatCommandDefinition("lang", "Show or change the interface language.", ChatCommandScope.Player, false, ["language"], null),
            new ChatCommandDefinition("wave", "Trigger the wave actor status.", ChatCommandScope.Player, true, [], null),
            new ChatCommandDefinition("sit", "Set the actor posture to sitting.", ChatCommandScope.Player, true, [], null),
            new ChatCommandDefinition("lay", "Set the actor posture to laying.", ChatCommandScope.Player, true, [], null),
            new ChatCommandDefinition("stand", "Clear posture statuses and stand up.", ChatCommandScope.Player, true, [], null),
            new ChatCommandDefinition("whisper", "Send a private-style room whisper to a specific user.", ChatCommandScope.Player, true, ["tell"], null),
            new ChatCommandDefinition("shout", "Broadcast a louder room message.", ChatCommandScope.Player, true, ["speak"], null),
            new ChatCommandDefinition("roommute", "Mute or unmute the current room chat.", ChatCommandScope.RoomModerator, true, [], StaffCapabilityKeys.RoomMute),
            new ChatCommandDefinition("roomalert", "Broadcast a visible alert to the current room.", ChatCommandScope.RoomModerator, true, ["ra"], StaffCapabilityKeys.RoomAlert),
            new ChatCommandDefinition("pickall", "Return every floor item in the current room to storage.", ChatCommandScope.RoomModerator, true, [], StaffCapabilityKeys.RoomPickAll),
            new ChatCommandDefinition("ha", "Queue a hotel-wide alert.", ChatCommandScope.HotelModerator, false, ["hotelalert", "alert"], StaffCapabilityKeys.HotelAlert),
            new ChatCommandDefinition("alert", "Send a moderation alert to a room-present user.", ChatCommandScope.HotelModerator, true, [], StaffCapabilityKeys.HotelModTool),
            new ChatCommandDefinition("kick", "Kick a room-present user.", ChatCommandScope.HotelModerator, true, [], StaffCapabilityKeys.HotelModTool),
            new ChatCommandDefinition("softkick", "Soft-kick a room-present user.", ChatCommandScope.HotelModerator, true, [], StaffCapabilityKeys.HotelModTool),
            new ChatCommandDefinition("shutup", "Mute a room-present user.", ChatCommandScope.HotelModerator, true, [], StaffCapabilityKeys.HotelModTool),
            new ChatCommandDefinition("unmute", "Unmute a room-present user.", ChatCommandScope.HotelModerator, true, [], StaffCapabilityKeys.HotelModTool),
            new ChatCommandDefinition("ban", "Ban a room-present user for a number of seconds.", ChatCommandScope.HotelModerator, true, [], StaffCapabilityKeys.HotelBan),
            new ChatCommandDefinition("superban", "Permanently ban a room-present user.", ChatCommandScope.HotelModerator, true, [], StaffCapabilityKeys.HotelBan),
            new ChatCommandDefinition("transfer", "Transfer credits to a room-present user.", ChatCommandScope.HotelModerator, true, [], StaffCapabilityKeys.HotelTransfer),
            new ChatCommandDefinition("rareweek", "Show, enable, disable, or change the rare of the week offer.", ChatCommandScope.Administrator, false, ["rotw"], StaffCapabilityKeys.CatalogManage),
            new ChatCommandDefinition("gamesessions", "List or inspect active game sessions.", ChatCommandScope.Administrator, false, ["games"], StaffCapabilityKeys.GamesManage),
            new ChatCommandDefinition("bbprepare", "Prepare a BattleBall session for a new round.", ChatCommandScope.Administrator, false, ["bbprep"], StaffCapabilityKeys.GamesManage),
            new ChatCommandDefinition("bbstart", "Start a BattleBall round.", ChatCommandScope.Administrator, false, [], StaffCapabilityKeys.GamesManage),
            new ChatCommandDefinition("bbscore", "Award points to a BattleBall team.", ChatCommandScope.Administrator, false, [], StaffCapabilityKeys.GamesManage),
            new ChatCommandDefinition("bbfinish", "Finish a BattleBall match.", ChatCommandScope.Administrator, false, [], StaffCapabilityKeys.GamesManage),
            new ChatCommandDefinition("ssprepare", "Prepare a SnowStorm session for a new battle.", ChatCommandScope.Administrator, false, ["ssprep"], StaffCapabilityKeys.GamesManage),
            new ChatCommandDefinition("ssstart", "Start a SnowStorm battle.", ChatCommandScope.Administrator, false, [], StaffCapabilityKeys.GamesManage),
            new ChatCommandDefinition("ssscore", "Award points to a SnowStorm team.", ChatCommandScope.Administrator, false, [], StaffCapabilityKeys.GamesManage),
            new ChatCommandDefinition("ssfinish", "Finish a SnowStorm match.", ChatCommandScope.Administrator, false, [], StaffCapabilityKeys.GamesManage),
            new ChatCommandDefinition("wsprepare", "Prepare a Wobble Squabble duel session.", ChatCommandScope.Administrator, false, ["wsprep"], StaffCapabilityKeys.GamesManage),
            new ChatCommandDefinition("wsstart", "Start a Wobble Squabble duel.", ChatCommandScope.Administrator, false, [], StaffCapabilityKeys.GamesManage),
            new ChatCommandDefinition("wsscore", "Award points to a Wobble Squabble team.", ChatCommandScope.Administrator, false, [], StaffCapabilityKeys.GamesManage),
            new ChatCommandDefinition("wsfinish", "Finish a Wobble Squabble duel.", ChatCommandScope.Administrator, false, [], StaffCapabilityKeys.GamesManage),
            new ChatCommandDefinition("kickall", "Evict all players from a room or all active rooms.", ChatCommandScope.Administrator, false, ["evictall"], StaffCapabilityKeys.EmergencyKickAll),
            new ChatCommandDefinition("lockdown", "Toggle hotel-wide lockdown to block new room entries.", ChatCommandScope.Administrator, false, [], StaffCapabilityKeys.EmergencyLockdown),
            new ChatCommandDefinition("maintenance", "Activate maintenance mode: broadcast alert and lock the hotel.", ChatCommandScope.Administrator, false, ["maint"], StaffCapabilityKeys.EmergencyLockdown)
        ]);

        store.AccessCapabilities.AddRange(
        [
            new AccessCapability(StaffCapabilityKeys.HotelAlert, "Send a hotel-wide alert."),
            new AccessCapability(StaffCapabilityKeys.HotelModTool, "Access moderation tooling."),
            new AccessCapability(StaffCapabilityKeys.HotelBan, "Ban room-present users from hotel access."),
            new AccessCapability(StaffCapabilityKeys.HotelTransfer, "Transfer credits to a room-present user."),
            new AccessCapability(StaffCapabilityKeys.RoomMute, "Mute or unmute a room."),
            new AccessCapability(StaffCapabilityKeys.RoomAlert, "Broadcast an alert inside the current room."),
            new AccessCapability(StaffCapabilityKeys.RoomPickAll, "Return all room items to storage."),
            new AccessCapability(StaffCapabilityKeys.CatalogManage, "Manage featured catalog states and highlighted offers."),
            new AccessCapability(StaffCapabilityKeys.CatalogReload, "Reload catalog content."),
            new AccessCapability(StaffCapabilityKeys.GamesManage, "Prepare, start, score, and finish managed game sessions."),
            new AccessCapability(StaffCapabilityKeys.HousekeepingAccess, "Access housekeeping surfaces."),
            new AccessCapability(StaffCapabilityKeys.EmergencyLockdown, "Activate or deactivate hotel-wide lockdown and maintenance mode."),
            new AccessCapability(StaffCapabilityKeys.EmergencyKickAll, "Evict all players from one or all active rooms.")
        ]);

        store.StaffRoleDefinitions.AddRange(
        [
            new StaffRoleDefinition("player", "Player", 0, ChatCommandScope.Player, []),
            new StaffRoleDefinition("hobba", "Rank 1 Room Guardian", 1, ChatCommandScope.RoomModerator, [StaffCapabilityKeys.RoomMute, StaffCapabilityKeys.RoomAlert]),
            new StaffRoleDefinition("lince", "Rank 2 Hotel Guardian", 2, ChatCommandScope.RoomModerator, [StaffCapabilityKeys.RoomMute, StaffCapabilityKeys.RoomAlert, StaffCapabilityKeys.HotelModTool]),
            new StaffRoleDefinition("moderator", "Rank 3 Moderator", 3, ChatCommandScope.HotelModerator, [StaffCapabilityKeys.RoomMute, StaffCapabilityKeys.RoomAlert, StaffCapabilityKeys.HotelModTool, StaffCapabilityKeys.HotelAlert, StaffCapabilityKeys.HotelBan, StaffCapabilityKeys.HotelTransfer]),
            new StaffRoleDefinition("administrator", "Rank 4 Administrator", 4, ChatCommandScope.Administrator, [StaffCapabilityKeys.RoomMute, StaffCapabilityKeys.RoomAlert, StaffCapabilityKeys.RoomPickAll, StaffCapabilityKeys.HotelModTool, StaffCapabilityKeys.HotelAlert, StaffCapabilityKeys.HotelBan, StaffCapabilityKeys.HotelTransfer, StaffCapabilityKeys.CatalogManage, StaffCapabilityKeys.CatalogReload, StaffCapabilityKeys.GamesManage, StaffCapabilityKeys.HousekeepingAccess, StaffCapabilityKeys.EmergencyLockdown, StaffCapabilityKeys.EmergencyKickAll]),
            new StaffRoleDefinition("manager", "Rank 5 Manager", 5, ChatCommandScope.Administrator, [StaffCapabilityKeys.RoomMute, StaffCapabilityKeys.RoomAlert, StaffCapabilityKeys.RoomPickAll, StaffCapabilityKeys.HotelModTool, StaffCapabilityKeys.HotelAlert, StaffCapabilityKeys.HotelBan, StaffCapabilityKeys.HotelTransfer, StaffCapabilityKeys.CatalogManage, StaffCapabilityKeys.CatalogReload, StaffCapabilityKeys.GamesManage, StaffCapabilityKeys.HousekeepingAccess, StaffCapabilityKeys.EmergencyLockdown, StaffCapabilityKeys.EmergencyKickAll]),
            new StaffRoleDefinition("owner", "Rank 6 Owner", 6, ChatCommandScope.Administrator, [StaffCapabilityKeys.RoomMute, StaffCapabilityKeys.RoomAlert, StaffCapabilityKeys.RoomPickAll, StaffCapabilityKeys.HotelModTool, StaffCapabilityKeys.HotelAlert, StaffCapabilityKeys.HotelBan, StaffCapabilityKeys.HotelTransfer, StaffCapabilityKeys.CatalogManage, StaffCapabilityKeys.CatalogReload, StaffCapabilityKeys.GamesManage, StaffCapabilityKeys.HousekeepingAccess, StaffCapabilityKeys.EmergencyLockdown, StaffCapabilityKeys.EmergencyKickAll])
        ]);

        store.StaffRoleAssignments[characterId] =
        [
            new StaffRoleAssignment(characterId, "owner", true, DateTime.UtcNow.AddMonths(-6)),
            new StaffRoleAssignment(characterId, "administrator", false, DateTime.UtcNow.AddMonths(-7))
        ];

        store.StaffRoleAssignments[roomGuardianCharacterId] =
        [
            new StaffRoleAssignment(roomGuardianCharacterId, "hobba", true, DateTime.UtcNow.AddMonths(-2))
        ];

        store.StaffRoleAssignments[linceCharacterId] =
        [
            new StaffRoleAssignment(linceCharacterId, "lince", true, DateTime.UtcNow.AddMonths(-2))
        ];

        store.StaffRoleAssignments[moderatorCharacterId] =
        [
            new StaffRoleAssignment(moderatorCharacterId, "moderator", true, DateTime.UtcNow.AddMonths(-2))
        ];

        store.StaffRoleAssignments[administratorCharacterId] =
        [
            new StaffRoleAssignment(administratorCharacterId, "administrator", true, DateTime.UtcNow.AddMonths(-2))
        ];

        store.StaffRoleAssignments[managerCharacterId] =
        [
            new StaffRoleAssignment(managerCharacterId, "manager", true, DateTime.UtcNow.AddMonths(-2))
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

        store.Rooms[new RoomId(10)] = new RoomDefinition(
            RoomId: new RoomId(10),
            RoomKind: RoomKind.Public,
            OwnerCharacterId: null,
            Name: "Lido Deck",
            Description: "Classic public pool with swim and dive behaviors.",
            CategoryId: 1,
            LayoutCode: "newbie_lobby",
            Settings: new RoomSettings(RoomAccessMode.Open, null, 50, true, false, false, false),
            Tags: ["lido", "public", "pool"]);

        store.Rooms[new RoomId(11)] = new RoomDefinition(
            RoomId: new RoomId(11),
            RoomKind: RoomKind.Public,
            OwnerCharacterId: null,
            Name: "BattleBall Stadium",
            Description: "Official team arena for BattleBall rounds.",
            CategoryId: 2,
            LayoutCode: "newbie_lobby",
            Settings: new RoomSettings(RoomAccessMode.Open, null, 50, true, false, false, false),
            Tags: ["battleball", "game", "public"]);

        store.Rooms[new RoomId(12)] = new RoomDefinition(
            RoomId: new RoomId(12),
            RoomKind: RoomKind.Public,
            OwnerCharacterId: null,
            Name: "SnowStorm Arena",
            Description: "Official SnowStorm combat arena.",
            CategoryId: 2,
            LayoutCode: "newbie_lobby",
            Settings: new RoomSettings(RoomAccessMode.Open, null, 50, true, false, false, false),
            Tags: ["snowstorm", "game", "public"]);

        store.Rooms[new RoomId(13)] = new RoomDefinition(
            RoomId: new RoomId(13),
            RoomKind: RoomKind.Public,
            OwnerCharacterId: null,
            Name: "Wobble Squabble Hall",
            Description: "Official duel venue for Wobble Squabble.",
            CategoryId: 2,
            LayoutCode: "newbie_lobby",
            Settings: new RoomSettings(RoomAccessMode.Open, null, 50, true, false, false, false),
            Tags: ["wobble", "gamehall", "public"]);

        store.PublicRoomPackages["welcome_lobby_core"] = new PublicRoomPackageDefinition(
            AssetPackageKey: "welcome_lobby_core",
            AssetFamily: "public_room",
            VisualProfileKey: "default_public_room",
            BaseLayoutCode: "newbie_lobby",
            AssetLayerKeys: ["background", "props", "lighting", "entry"],
            Tags: ["welcome", "lobby"]);

        store.PublicRoomPackages["lido_deck_core"] = new PublicRoomPackageDefinition(
            AssetPackageKey: "lido_deck_core",
            AssetFamily: "public_room",
            VisualProfileKey: "classic_lido",
            BaseLayoutCode: "newbie_lobby",
            AssetLayerKeys: ["background", "water", "props", "lighting"],
            Tags: ["lido", "pool", "public"]);

        store.PublicRoomPackages["battleball_stadium_core"] = new PublicRoomPackageDefinition(
            AssetPackageKey: "battleball_stadium_core",
            AssetFamily: "public_room",
            VisualProfileKey: "arena_battleball",
            BaseLayoutCode: "newbie_lobby",
            AssetLayerKeys: ["background", "arena", "lighting"],
            Tags: ["battleball", "game"]);

        store.PublicRoomPackages["snowstorm_arena_core"] = new PublicRoomPackageDefinition(
            AssetPackageKey: "snowstorm_arena_core",
            AssetFamily: "public_room",
            VisualProfileKey: "arena_snowstorm",
            BaseLayoutCode: "newbie_lobby",
            AssetLayerKeys: ["background", "snow", "arena", "lighting"],
            Tags: ["snowstorm", "game"]);

        store.PublicRoomPackages["wobblesquabble_hall_core"] = new PublicRoomPackageDefinition(
            AssetPackageKey: "wobblesquabble_hall_core",
            AssetFamily: "public_room",
            VisualProfileKey: "arcade_wobble",
            BaseLayoutCode: "newbie_lobby",
            AssetLayerKeys: ["background", "stage", "props", "lighting"],
            Tags: ["wobblesquabble", "gamehall"]);

        store.PublicRoomPackages["infobus_theatre_core"] = new PublicRoomPackageDefinition(
            AssetPackageKey: "infobus_theatre_core",
            AssetFamily: "public_room",
            VisualProfileKey: "theatre_infobus",
            BaseLayoutCode: "newbie_lobby",
            AssetLayerKeys: ["background", "stage", "seating", "lighting"],
            Tags: ["infobus", "public", "theatre"]);

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

        store.NavigatorPublicRooms[2] = new NavigatorPublicRoomDefinition(
            EntryId: 2,
            OrderNumber: 2,
            BannerTypeCode: "lido",
            Caption: "Lido Deck",
            ImagePath: "lido_banner",
            ImageKind: "internal",
            RoomId: new RoomId(10),
            CategoryId: 1,
            ParentCategoryId: 0,
            AssetPackageKey: "lido_deck_core");

        store.NavigatorPublicRooms[3] = new NavigatorPublicRoomDefinition(
            EntryId: 3,
            OrderNumber: 3,
            BannerTypeCode: "battleball",
            Caption: "BattleBall Stadium",
            ImagePath: "battleball_banner",
            ImageKind: "internal",
            RoomId: new RoomId(11),
            CategoryId: 2,
            ParentCategoryId: 0,
            AssetPackageKey: "battleball_stadium_core");

        store.NavigatorPublicRooms[4] = new NavigatorPublicRoomDefinition(
            EntryId: 4,
            OrderNumber: 4,
            BannerTypeCode: "snowstorm",
            Caption: "SnowStorm Arena",
            ImagePath: "snowstorm_banner",
            ImageKind: "internal",
            RoomId: new RoomId(12),
            CategoryId: 2,
            ParentCategoryId: 0,
            AssetPackageKey: "snowstorm_arena_core");

        store.NavigatorPublicRooms[5] = new NavigatorPublicRoomDefinition(
            EntryId: 5,
            OrderNumber: 5,
            BannerTypeCode: "wobble",
            Caption: "Wobble Squabble Hall",
            ImagePath: "wobble_banner",
            ImageKind: "internal",
            RoomId: new RoomId(13),
            CategoryId: 2,
            ParentCategoryId: 0,
            AssetPackageKey: "wobblesquabble_hall_core");

        store.CatalogPages.AddRange(
        [
            new CatalogPageDefinition(
                new CatalogPageId(1),
                null,
                "Featured",
                0,
                1,
                true,
                true,
                0,
                false,
                1,
                "default_3x3",
                "Featured Picks",
                "Popular essentials for the lobby.",
                string.Empty,
                "Fresh furniture for active hotel development.",
                "Use credits and duckets to purchase items.",
                string.Empty,
                string.Empty),
            new CatalogPageDefinition(
                new CatalogPageId(2),
                null,
                "Mobis",
                0,
                2,
                true,
                true,
                0,
                false,
                2,
                "default_3x3",
                "Furniture",
                "Core room items.",
                string.Empty,
                "Sofas, plants, and utility room items.",
                "Stock your inventory before furnishing rooms.",
                string.Empty,
                string.Empty),
            new CatalogPageDefinition(
                new CatalogPageId(3),
                null,
                "Ecotron",
                0,
                3,
                true,
                true,
                0,
                false,
                3,
                "frontpage4",
                "Ecotron Recycling",
                "Reward loops and random furniture returns.",
                string.Empty,
                "Trade unwanted furni into randomized reward boxes.",
                "This category is prepared for recycler and eco-box flows.",
                string.Empty,
                string.Empty),
            new CatalogPageDefinition(
                new CatalogPageId(4),
                null,
                "Rares",
                0,
                4,
                true,
                true,
                0,
                false,
                4,
                "frontpage3",
                "Rare Releases",
                "Timed and collectible furni drops.",
                string.Empty,
                "Weekly rare drops and seasonal showcase items.",
                "High-identity catalog lane for special content.",
                string.Empty,
                string.Empty),
            new CatalogPageDefinition(
                new CatalogPageId(5),
                null,
                "Effects",
                0,
                5,
                true,
                true,
                0,
                false,
                5,
                "frontpage3",
                "Avatar Effects",
                "Temporary visual boosts and room presence effects.",
                string.Empty,
                "Unique effect catalog lane for cosmetic runtime features.",
                "Prepared for richer modern client previews.",
                string.Empty,
                string.Empty)
        ]);

        store.CatalogOffers.AddRange(
        [
            new CatalogOfferDefinition(
                new CatalogOfferId(1),
                new CatalogPageId(1),
                "Starter Sofa",
                CatalogOfferKind.Single,
                150,
                0,
                0,
                [new CatalogOfferProductDefinition(sofaDefinitionId, 1)]),
            new CatalogOfferDefinition(
                new CatalogOfferId(2),
                new CatalogPageId(1),
                "Lobby Plant Set",
                CatalogOfferKind.Bundle,
                75,
                25,
                0,
                [new CatalogOfferProductDefinition(plantDefinitionId, 2)]),
            new CatalogOfferDefinition(
                new CatalogOfferId(3),
                new CatalogPageId(2),
                "Navigator Teleporter",
                CatalogOfferKind.Single,
                300,
                0,
                0,
                [new CatalogOfferProductDefinition(teleporterDefinitionId, 1)]),
            new CatalogOfferDefinition(
                new CatalogOfferId(4),
                new CatalogPageId(4),
                "Rare Amber Podium",
                CatalogOfferKind.Single,
                500,
                0,
                0,
                [new CatalogOfferProductDefinition(plantDefinitionId, 1)]),
            new CatalogOfferDefinition(
                new CatalogOfferId(5),
                new CatalogPageId(3),
                "Collector Starter Deal",
                CatalogOfferKind.Bundle,
                225,
                50,
                0,
                [
                    new CatalogOfferProductDefinition(sofaDefinitionId, 1),
                    new CatalogOfferProductDefinition(plantDefinitionId, 3),
                    new CatalogOfferProductDefinition(teleporterDefinitionId, 1)
                ])
        ]);

        store.CatalogCampaigns.AddRange(EmbeddedSeedContentLoader.LoadCatalogCampaigns());
        foreach (CatalogFeatureState featureState in EmbeddedSeedContentLoader.LoadCatalogFeatureStates())
        {
            store.CatalogFeatureStates[featureState.FeatureKey] = featureState;
        }

        store.GameDefinitions.AddRange(EmbeddedSeedContentLoader.LoadGameDefinitions());
        store.GameVenues.AddRange(EmbeddedSeedContentLoader.LoadGameVenues());
        store.GameSessions.AddRange(
        [
            new GameSessionState(
                SessionKey: "battleball-public-1",
                GameKey: "battleball",
                VenueKey: "battleball_stadium",
                RoomId: new RoomId(11),
                Status: GameSessionStatus.Running,
                PhaseCode: "round_live",
                IsPrivateMatch: false,
                MaximumPlayers: 8,
                StartedAtUtc: DateTime.UtcNow.AddMinutes(-6),
                Teams:
                [
                    new GameTeamDefinition("red", "Red Team", "red", 42),
                    new GameTeamDefinition("blue", "Blue Team", "blue", 36)
                ],
                Players:
                [
                    new GamePlayerState(new CharacterId(1), "epsilon", "red", 18, true, DateTime.UtcNow.AddMinutes(-7)),
                    new GamePlayerState(new CharacterId(2), "delta", "red", 24, true, DateTime.UtcNow.AddMinutes(-7)),
                    new GamePlayerState(new CharacterId(3), "nova", "blue", 17, true, DateTime.UtcNow.AddMinutes(-7)),
                    new GamePlayerState(new CharacterId(4), "atlas", "blue", 19, true, DateTime.UtcNow.AddMinutes(-7))
                ]),
            new GameSessionState(
                SessionKey: "snowstorm-public-1",
                GameKey: "snowstorm",
                VenueKey: "snowstorm_arena",
                RoomId: new RoomId(12),
                Status: GameSessionStatus.Preparing,
                PhaseCode: "matchmaking",
                IsPrivateMatch: false,
                MaximumPlayers: 8,
                StartedAtUtc: DateTime.UtcNow.AddMinutes(-2),
                Teams:
                [
                    new GameTeamDefinition("north", "North Team", "ice_blue", 0),
                    new GameTeamDefinition("south", "South Team", "frost_white", 0)
                ],
                Players:
                [
                    new GamePlayerState(new CharacterId(5), "pixel", "north", 0, true, DateTime.UtcNow.AddMinutes(-2)),
                    new GamePlayerState(new CharacterId(6), "ember", "south", 0, true, DateTime.UtcNow.AddMinutes(-2))
                ]),
            new GameSessionState(
                SessionKey: "wobble-private-1",
                GameKey: "wobblesquabble",
                VenueKey: "wobble_squabble_hall",
                RoomId: new RoomId(13),
                Status: GameSessionStatus.Waiting,
                PhaseCode: "waiting_for_duelists",
                IsPrivateMatch: true,
                MaximumPlayers: 2,
                StartedAtUtc: DateTime.UtcNow.AddMinutes(-1),
                Teams:
                [
                    new GameTeamDefinition("left", "Left", "gold", 0),
                    new GameTeamDefinition("right", "Right", "silver", 0)
                ],
                Players:
                [
                    new GamePlayerState(new CharacterId(7), "vector", "left", 0, true, DateTime.UtcNow.AddMinutes(-1))
                ])
        ]);
        store.VoucherDefinitions.AddRange(EmbeddedSeedContentLoader.LoadVoucherDefinitions());
        store.CollectibleDefinitions.AddRange(EmbeddedSeedContentLoader.LoadCollectibleDefinitions());
        store.EcotronRewards.AddRange(EmbeddedSeedContentLoader.LoadEcotronRewards());
        store.PublicRoomBehaviors.AddRange(EmbeddedSeedContentLoader.LoadPublicRoomBehaviors());

        store.EffectDefinitions.AddRange(
        [
            new EffectDefinition(
                "gold_glow",
                "Gold Glow",
                "A soft golden aura for premium room presence.",
                7001,
                120,
                0,
                false,
                3600,
                "effect_glow_gold"),
            new EffectDefinition(
                "storm_spark",
                "Storm Spark",
                "Short electric burst trail for dramatic entries.",
                7002,
                0,
                80,
                false,
                1800,
                "effect_storm_spark"),
            new EffectDefinition(
                "vip_orbit",
                "VIP Orbit",
                "Premium orbiting particles around the avatar.",
                7003,
                250,
                0,
                true,
                5400,
                "effect_vip_orbit")
        ]);

        store.RoomVisualScenes.AddRange(
        [
            new RoomVisualSceneDefinition(
                "welcome_lobby_gold_hour",
                "newbie_lobby",
                "golden_city",
                "#E5A623",
                "soft_isometric_long",
                "warm_evening",
                "slow_cloud_band",
                true,
                true,
                ["cloud_drift", "window_glow", "city_haze"])
        ]);

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
