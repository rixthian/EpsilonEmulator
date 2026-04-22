using Epsilon.CoreGame;

namespace Epsilon.Games;

public sealed record GameVenueDefinition(
    string VenueKey,
    string GameKey,
    GameVenueKind VenueKind,
    RoomKind HostRoomKind,
    RoomId RoomId,
    string DisplayName,
    string AssetPackageKey,
    bool IsRecommended,
    IReadOnlyList<string> Tags);
