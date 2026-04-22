using Epsilon.Content;
using Epsilon.Rooms;

namespace Epsilon.CoreGame;

public sealed record NavigatorPublicRoomSnapshot(
    NavigatorPublicRoomDefinition Entry,
    RoomDefinition? Room);

public sealed record NavigatorSearchRequest(
    string? Query,
    bool RecommendedOnly);

public sealed record NavigatorSearchSnapshot(
    IReadOnlyList<NavigatorPublicRoomSnapshot> PublicRooms);
