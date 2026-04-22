using Epsilon.Rooms;

namespace Epsilon.CoreGame;

public sealed record RoomVisualSnapshot(
    RoomId RoomId,
    string RoomName,
    RoomVisualSceneDefinition? Scene,
    IReadOnlyList<string> ActiveAmbientEffectKeys);
