using Epsilon.Content;

namespace Epsilon.CoreGame;

public sealed record RoomEntrySnapshot(
    CharacterHotelSnapshot Character,
    RoomHotelSnapshot Room,
    NavigatorPublicRoomDefinition? PublicRoomEntry,
    bool CanManageRoom,
    bool SpectatorMode);
