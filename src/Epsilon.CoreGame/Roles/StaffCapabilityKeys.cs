namespace Epsilon.CoreGame;

public static class StaffCapabilityKeys
{
    public const string HotelAlert = "hotel.alert";
    public const string HotelModTool = "hotel.modtool";
    public const string HotelBan = "hotel.ban";
    public const string HotelTransfer = "hotel.transfer";
    public const string RoomMute = "room.mute";
    public const string RoomAlert = "room.alert";
    public const string RoomPickAll = "room.pick_all";
    public const string CatalogManage = "catalog.manage";
    public const string CatalogReload = "catalog.reload";
    public const string GamesManage = "games.manage";
    public const string HousekeepingAccess = "housekeeping.access";

    // Emergency capabilities — granted only to administrator, manager, and owner.
    public const string EmergencyLockdown = "emergency.lockdown";
    public const string EmergencyKickAll = "emergency.kick_all";
}
