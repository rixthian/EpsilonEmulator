namespace Epsilon.CoreGame;

public sealed class HotelBootstrapService : IHotelBootstrapService
{
    private readonly IHotelReadService _hotelReadService;

    public HotelBootstrapService(IHotelReadService hotelReadService)
    {
        _hotelReadService = hotelReadService;
    }

    public async ValueTask<HotelBootstrapSnapshot?> BuildAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        CharacterHotelSnapshot? character = await _hotelReadService.GetCharacterSnapshotAsync(characterId, cancellationToken);

        if (character is null)
        {
            return null;
        }

        RoomHotelSnapshot? homeRoom = await _hotelReadService.GetRoomSnapshotAsync(
            character.Profile.HomeRoomId,
            cancellationToken);

        return new HotelBootstrapSnapshot(character, homeRoom);
    }
}

