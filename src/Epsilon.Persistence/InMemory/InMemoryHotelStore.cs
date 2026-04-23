using Epsilon.Content;
using Epsilon.CoreGame;
using Epsilon.Games;
using Epsilon.Rooms;

namespace Epsilon.Persistence;

internal sealed class InMemoryHotelStore
{
    public Dictionary<AccountId, AccountRecord> Accounts { get; } = [];
    public Dictionary<CharacterId, CharacterProfile> Characters { get; } = [];
    public long NextAccountId { get; set; } = 100;
    public long NextCharacterId { get; set; } = 100;
    public Dictionary<CharacterId, List<SubscriptionRecord>> Subscriptions { get; } = [];
    public Dictionary<CharacterId, List<PetProfile>> Pets { get; } = [];
    public Dictionary<CharacterId, WalletSnapshot> Wallets { get; } = [];
    public Dictionary<CharacterId, List<WalletLinkSnapshot>> WalletLinks { get; } = [];
    public Dictionary<CharacterId, CollectibleOwnershipSnapshot> CollectibleOwnerships { get; } = [];
    public Dictionary<CharacterId, CollectorProgressSnapshot> CollectorProgress { get; } = [];
    public Dictionary<CharacterId, List<string>> PendingGiftBoxes { get; } = [];
    public Dictionary<CharacterId, Dictionary<string, DateTime>> FactoryClaims { get; } = [];
    public List<MarketListingState> MarketListings { get; } = [];
    public Dictionary<CharacterId, CharacterInterfacePreference> InterfacePreferences { get; } = [];
    public Dictionary<CharacterId, CharacterChatFilterPreference> ChatFilterPreferences { get; } = [];
    public Dictionary<CharacterId, List<InventoryItemState>> InventoryItems { get; } = [];
    public Dictionary<CharacterId, List<MessengerContact>> MessengerContacts { get; } = [];
    public Dictionary<CharacterId, List<MessengerRequest>> PendingMessengerRequests { get; } = [];
    public Dictionary<GroupId, HotelGroup> Groups { get; } = [];
    public Dictionary<GroupId, List<GroupMembership>> GroupMemberships { get; } = [];
    public Dictionary<CharacterId, List<BadgeAssignment>> BadgeAssignments { get; } = [];
    public Dictionary<CharacterId, List<AchievementProgress>> AchievementProgress { get; } = [];
    public List<ChatCommandDefinition> ChatCommandCatalog { get; } = [];
    public Dictionary<CharacterId, List<StaffRoleAssignment>> StaffRoleAssignments { get; } = [];
    public Dictionary<CharacterId, ModerationBanRecord> ModerationBans { get; } = [];
    public List<HotelBotDefinition> BotDefinitions { get; } = [];
    public List<StaffRoleDefinition> StaffRoleDefinitions { get; } = [];
    public List<AccessCapability> AccessCapabilities { get; } = [];
    public List<HotelAdvertisement> Advertisements { get; } = [];
    public List<SupportTopicCategory> SupportCategories { get; } = [];
    public List<SupportTopicEntry> SupportTopics { get; } = [];
    public List<SupportCallTicket> SupportCallTickets { get; } = [];
    public Dictionary<RoomId, RoomDefinition> Rooms { get; } = [];
    public Dictionary<string, RoomLayoutDefinition> Layouts { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<RoomId, List<RoomItemState>> RoomItems { get; } = [];
    public Dictionary<RoomId, List<RoomActorState>> RoomActors { get; } = [];
    public Dictionary<RoomId, RoomActivitySnapshot> RoomActivities { get; } = [];
    public Dictionary<RoomId, RoomChatPolicySnapshot> RoomChatPolicies { get; } = [];
    public Dictionary<RoomId, List<RoomChatMessage>> RoomChatMessages { get; } = [];
    public Dictionary<RoomId, List<RoomChatMessage>> PrivateRoomChatMessages { get; } = [];
    public Dictionary<ItemDefinitionId, ItemDefinition> ItemDefinitions { get; } = [];
    public List<CatalogPageDefinition> CatalogPages { get; } = [];
    public List<CatalogOfferDefinition> CatalogOffers { get; } = [];
    public List<CatalogCampaignDefinition> CatalogCampaigns { get; } = [];
    public Dictionary<string, CatalogFeatureState> CatalogFeatureStates { get; } =
        new(StringComparer.OrdinalIgnoreCase);
    public List<InterfaceLanguageDefinition> InterfaceLanguages { get; } = [];
    public List<BadgeDefinition> BadgeDefinitions { get; } = [];
    public List<GameDefinition> GameDefinitions { get; } = [];
    public List<GameVenueDefinition> GameVenues { get; } = [];
    public List<GameSessionState> GameSessions { get; } = [];
    public HabbowoodEventDefinition? HabbowoodDefinition { get; set; }
    public List<HabbowoodAssetPackage> HabbowoodAssetPackages { get; } = [];
    public List<HabbowoodMovieSubmission> HabbowoodSubmissions { get; } = [];
    public List<HabbowoodVoteLedgerEntry> HabbowoodVotes { get; } = [];
    public List<VoucherDefinition> VoucherDefinitions { get; } = [];
    public Dictionary<CharacterId, HashSet<string>> RedeemedVoucherCodes { get; } = [];
    public List<CollectibleDefinition> CollectibleDefinitions { get; } = [];
    public List<EcotronRewardDefinition> EcotronRewards { get; } = [];
    public List<EffectDefinition> EffectDefinitions { get; } = [];
    public List<RoomVisualSceneDefinition> RoomVisualScenes { get; } = [];
    public List<PublicRoomBehaviorDefinition> PublicRoomBehaviors { get; } = [];
    public Dictionary<int, NavigatorPublicRoomDefinition> NavigatorPublicRooms { get; } = [];
    public Dictionary<string, PublicRoomPackageDefinition> PublicRoomPackages { get; } =
        new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, ClientPackageManifest> ClientPackages { get; } =
        new(StringComparer.OrdinalIgnoreCase);
    public long NextItemId { get; set; } = 10000;
    public long NextGroupId { get; set; } = 100;
    public long NextHabbowoodSubmissionId { get; set; } = 1000;
    public long NextMarketListingId { get; set; } = 1;
}
