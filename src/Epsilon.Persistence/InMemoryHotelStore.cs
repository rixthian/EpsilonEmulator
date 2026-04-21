using Epsilon.Content;
using Epsilon.CoreGame;
using Epsilon.Rooms;

namespace Epsilon.Persistence;

internal sealed class InMemoryHotelStore
{
    public Dictionary<CharacterId, CharacterProfile> Characters { get; } = [];
    public Dictionary<CharacterId, List<SubscriptionRecord>> Subscriptions { get; } = [];
    public Dictionary<CharacterId, List<PetProfile>> Pets { get; } = [];
    public Dictionary<CharacterId, WalletSnapshot> Wallets { get; } = [];
    public Dictionary<CharacterId, List<MessengerContact>> MessengerContacts { get; } = [];
    public Dictionary<CharacterId, List<MessengerRequest>> PendingMessengerRequests { get; } = [];
    public Dictionary<CharacterId, List<BadgeAssignment>> BadgeAssignments { get; } = [];
    public Dictionary<CharacterId, List<AchievementProgress>> AchievementProgress { get; } = [];
    public Dictionary<CharacterId, List<ChatCommandDefinition>> ChatCommands { get; } = [];
    public Dictionary<CharacterId, List<StaffRoleAssignment>> StaffRoleAssignments { get; } = [];
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
    public Dictionary<ItemDefinitionId, ItemDefinition> ItemDefinitions { get; } = [];
    public Dictionary<int, NavigatorPublicRoomDefinition> NavigatorPublicRooms { get; } = [];
    public Dictionary<string, PublicRoomAssetPackageDefinition> PublicRoomAssetPackages { get; } =
        new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, ClientPackageManifest> ClientPackages { get; } =
        new(StringComparer.OrdinalIgnoreCase);
}
