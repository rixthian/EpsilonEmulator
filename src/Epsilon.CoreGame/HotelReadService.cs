using Epsilon.Content;
using Epsilon.Rooms;

namespace Epsilon.CoreGame;

public sealed class HotelReadService : IHotelReadService
{
    private readonly ICharacterProfileRepository _characterProfiles;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IPetProfileRepository _pets;
    private readonly IRoomRepository _rooms;
    private readonly IRoomLayoutRepository _layouts;
    private readonly IRoomItemRepository _roomItems;
    private readonly IItemDefinitionRepository _itemDefinitions;
    private readonly INavigatorPublicRoomRepository _navigatorPublicRooms;
    private readonly IPublicRoomAssetPackageRepository _publicRoomAssetPackages;

    public HotelReadService(
        ICharacterProfileRepository characterProfiles,
        ISubscriptionRepository subscriptions,
        IPetProfileRepository pets,
        IRoomRepository rooms,
        IRoomLayoutRepository layouts,
        IRoomItemRepository roomItems,
        IItemDefinitionRepository itemDefinitions,
        INavigatorPublicRoomRepository navigatorPublicRooms,
        IPublicRoomAssetPackageRepository publicRoomAssetPackages)
    {
        _characterProfiles = characterProfiles;
        _subscriptions = subscriptions;
        _pets = pets;
        _rooms = rooms;
        _layouts = layouts;
        _roomItems = roomItems;
        _itemDefinitions = itemDefinitions;
        _navigatorPublicRooms = navigatorPublicRooms;
        _publicRoomAssetPackages = publicRoomAssetPackages;
    }

    public async ValueTask<CharacterHotelSnapshot?> GetCharacterSnapshotAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        CharacterProfile? profile = await _characterProfiles.GetByIdAsync(characterId, cancellationToken);

        if (profile is null)
        {
            return null;
        }

        IReadOnlyList<SubscriptionRecord> subscriptions = await _subscriptions.GetByCharacterIdAsync(characterId, cancellationToken);
        IReadOnlyList<PetProfile> pets = await _pets.GetByCharacterIdAsync(characterId, cancellationToken);

        return new CharacterHotelSnapshot(profile, subscriptions, pets);
    }

    public async ValueTask<RoomHotelSnapshot?> GetRoomSnapshotAsync(
        RoomId roomId,
        CancellationToken cancellationToken = default)
    {
        RoomDefinition? room = await _rooms.GetByIdAsync(roomId, cancellationToken);

        if (room is null)
        {
            return null;
        }

        RoomLayoutDefinition? layout = await _layouts.GetByCodeAsync(room.LayoutCode, cancellationToken);
        IReadOnlyList<RoomItemState> itemStates = await _roomItems.GetByRoomIdAsync(roomId, cancellationToken);

        List<RoomItemSnapshot> items = [];
        foreach (RoomItemState itemState in itemStates)
        {
            ItemDefinition? definition = await _itemDefinitions.GetByIdAsync(itemState.ItemDefinitionId, cancellationToken);
            items.Add(new RoomItemSnapshot(itemState, definition));
        }

        return new RoomHotelSnapshot(room, layout, items);
    }

    public async ValueTask<PublicRoomHotelSnapshot?> GetPublicRoomSnapshotAsync(
        int entryId,
        CancellationToken cancellationToken = default)
    {
        NavigatorPublicRoomDefinition? entry = await _navigatorPublicRooms.GetByEntryIdAsync(entryId, cancellationToken);

        if (entry is null)
        {
            return null;
        }

        PublicRoomAssetPackageDefinition? assetPackage =
            await _publicRoomAssetPackages.GetByKeyAsync(entry.AssetPackageKey, cancellationToken);
        RoomHotelSnapshot? room = await GetRoomSnapshotAsync(entry.RoomId, cancellationToken);

        return new PublicRoomHotelSnapshot(entry, assetPackage, room);
    }
}
