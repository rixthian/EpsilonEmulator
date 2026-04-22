using Epsilon.Content;
using Epsilon.Games;
using Epsilon.Rooms;

namespace Epsilon.CoreGame;

public sealed record GameCatalogSnapshot(
    IReadOnlyList<GameDefinition> Games,
    IReadOnlyList<GameVenueDefinition> Venues);

public sealed record GameRuntimeCatalogSnapshot(
    IReadOnlyList<GameSessionState> ActiveSessions);

public sealed record PublicRoomBehaviorSnapshot(
    NavigatorPublicRoomDefinition PublicRoom,
    IReadOnlyList<PublicRoomBehaviorDefinition> Behaviors);
