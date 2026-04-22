namespace Epsilon.CoreGame;

public sealed record HotelBootstrapSnapshot(
    CharacterHotelSnapshot Character,
    RoomHotelSnapshot? HomeRoom);

