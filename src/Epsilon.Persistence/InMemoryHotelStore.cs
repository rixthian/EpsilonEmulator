using Epsilon.Content;
using Epsilon.CoreGame;
using Epsilon.Rooms;

namespace Epsilon.Persistence;

internal sealed class InMemoryHotelStore
{
    public Dictionary<CharacterId, CharacterProfile> Characters { get; } = [];
    public Dictionary<CharacterId, List<SubscriptionRecord>> Subscriptions { get; } = [];
    public Dictionary<CharacterId, List<PetProfile>> Pets { get; } = [];
    public Dictionary<RoomId, RoomDefinition> Rooms { get; } = [];
    public Dictionary<string, RoomLayoutDefinition> Layouts { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<RoomId, List<RoomItemState>> RoomItems { get; } = [];
    public Dictionary<ItemDefinitionId, ItemDefinition> ItemDefinitions { get; } = [];
    public Dictionary<int, NavigatorPublicRoomDefinition> NavigatorPublicRooms { get; } = [];
    public Dictionary<string, PublicRoomAssetPackageDefinition> PublicRoomAssetPackages { get; } =
        new(StringComparer.OrdinalIgnoreCase);
}
